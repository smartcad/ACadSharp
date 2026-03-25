using System;
using System.IO;

namespace ACadSharp.IO.DWG
{
	/// <summary>
	/// Variation of the algorithm LZ77 used in 2004 DWG files.
	/// </summary>
	internal static class DwgLZ77AC18Decompressor
	{
		/// <summary>
		/// Decompress a stream with a specific decompressed size.
		/// </summary>
		public static Stream Decompress(Stream compressed, long decompressedSize)
		{
			byte[] dstBuf = new byte[decompressedSize];

			if (compressed is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> srcSeg))
			{
				int srcPos = srcSeg.Offset + (int)ms.Position;
				int dstPos = 0;
				DecompressBuffered(srcSeg.Array, ref srcPos, dstBuf, ref dstPos);
				ms.Position = srcPos - srcSeg.Offset;
			}
			else
			{
				var memDst = new MemoryStream(dstBuf);
				DecompressToDestStream(compressed, memDst);
			}

			return new MemoryStream(dstBuf, 0, dstBuf.Length, false, true);
		}

		/// <summary>
		/// Decompress source stream to destination stream.
		/// Uses direct byte[] access when both streams are MemoryStreams (avoids virtual dispatch on Mono).
		/// </summary>
		public static void DecompressToDest(Stream src, Stream dst)
		{
			// Fast path: both streams backed by byte[]
			if (src is MemoryStream srcMs && dst is MemoryStream dstMs
				&& srcMs.TryGetBuffer(out ArraySegment<byte> srcSeg)
				&& dstMs.TryGetBuffer(out ArraySegment<byte> dstSeg))
			{
				int srcPos = srcSeg.Offset + (int)srcMs.Position;
				int dstPos = dstSeg.Offset + (int)dstMs.Position;
				DecompressBuffered(srcSeg.Array, ref srcPos, dstSeg.Array, ref dstPos);
				srcMs.Position = srcPos - srcSeg.Offset;
				dstMs.Position = dstPos - dstSeg.Offset;
				return;
			}

			DecompressToDestStream(src, dst);
		}

		/// <summary>
		/// High-performance byte[]-based decompression — no virtual dispatch overhead.
		/// </summary>
		internal static void DecompressBuffered(byte[] src, ref int srcPos, byte[] dst, ref int dstPos)
		{
			int opcode1 = src[srcPos++];

			if ((opcode1 & 0xF0) == 0)
				opcode1 = CopyBuf(LiteralCountBuf(opcode1, src, ref srcPos) + 3, src, ref srcPos, dst, ref dstPos);

			while (opcode1 != 0x11)
			{
				int compOffset = 0;
				int compressedBytes = 0;

				if (opcode1 < 0x10 || opcode1 >= 0x40)
				{
					compressedBytes = (opcode1 >> 4) - 1;
					byte opcode2 = src[srcPos++];
					compOffset = ((opcode1 >> 2 & 3) | (opcode2 << 2)) + 1;
				}
				else if (opcode1 < 0x20)
				{
					compressedBytes = ReadCompressedBytesBuf(opcode1, 0b0111, src, ref srcPos);
					compOffset = (opcode1 & 8) << 11;
					opcode1 = TwoByteOffsetBuf(ref compOffset, 0x4000, src, ref srcPos);
				}
				else if (opcode1 >= 0x20)
				{
					compressedBytes = ReadCompressedBytesBuf(opcode1, 0b00011111, src, ref srcPos);
					opcode1 = TwoByteOffsetBuf(ref compOffset, 1, src, ref srcPos);
				}

				// Copy from earlier in dst — direct array access (no virtual dispatch)
				for (int i = 0; i < compressedBytes; ++i)
				{
					dst[dstPos + i] = dst[dstPos + i - compOffset];
				}
				dstPos += compressedBytes;

				int litCount = opcode1 & 3;
				if (litCount == 0)
				{
					opcode1 = src[srcPos++];
					if ((opcode1 & 0b11110000) == 0)
						litCount = LiteralCountBuf(opcode1, src, ref srcPos) + 3;
				}

				if (litCount > 0)
					opcode1 = CopyBuf(litCount, src, ref srcPos, dst, ref dstPos);
			}
		}

		private static byte CopyBuf(int count, byte[] src, ref int srcPos, byte[] dst, ref int dstPos)
		{
			Buffer.BlockCopy(src, srcPos, dst, dstPos, count);
			srcPos += count;
			dstPos += count;
			return src[srcPos++];
		}

		private static int LiteralCountBuf(int code, byte[] src, ref int srcPos)
		{
			int lowbits = code & 0b1111;
			if (lowbits == 0)
			{
				byte lastByte;
				for (lastByte = src[srcPos++]; lastByte == 0; lastByte = src[srcPos++])
					lowbits += 0xFF;
				lowbits += 0xF + lastByte;
			}
			return lowbits;
		}

		private static int ReadCompressedBytesBuf(int opcode1, int validBits, byte[] src, ref int srcPos)
		{
			int compressedBytes = opcode1 & validBits;
			if (compressedBytes == 0)
			{
				byte lastByte;
				for (lastByte = src[srcPos++]; lastByte == 0; lastByte = src[srcPos++])
					compressedBytes += 0xFF;
				compressedBytes += lastByte + validBits;
			}
			return compressedBytes + 2;
		}

		private static int TwoByteOffsetBuf(ref int offset, int addedValue, byte[] src, ref int srcPos)
		{
			int firstByte = src[srcPos++];
			offset |= firstByte >> 2;
			offset |= src[srcPos++] << 6;
			offset += addedValue;
			return firstByte;
		}

		#region Stream-based fallback (for non-MemoryStream sources)

		private static void DecompressToDestStream(Stream src, Stream dst)
		{
			int opcode1 = (byte)src.ReadByte();

			if ((opcode1 & 0xF0) == 0)
				opcode1 = copy(literalCount(opcode1, src) + 3, src, dst);

			while (opcode1 != 0x11)
			{
				int compOffset = 0;
				int compressedBytes = 0;

				if (opcode1 < 0x10 || opcode1 >= 0x40)
				{
					compressedBytes = (opcode1 >> 4) - 1;
					byte opcode2 = (byte)src.ReadByte();
					compOffset = ((opcode1 >> 2 & 3) | (opcode2 << 2)) + 1;
				}
				else if (opcode1 < 0x20)
				{
					compressedBytes = readCompressedBytes(opcode1, 0b0111, src);
					compOffset = (opcode1 & 8) << 11;
					opcode1 = twoByteOffset(ref compOffset, 0x4000, src);
				}
				else if (opcode1 >= 0x20)
				{
					compressedBytes = readCompressedBytes(opcode1, 0b00011111, src);
					opcode1 = twoByteOffset(ref compOffset, 1, src);
				}

				long position = dst.Position;
				for (long i = compressedBytes + position; position < i; ++position)
				{
					dst.Position = position - compOffset;
					byte value = (byte)dst.ReadByte();
					dst.Position = position;
					dst.WriteByte(value);
				}

				int litCount = opcode1 & 3;
				if (litCount == 0)
				{
					opcode1 = (byte)src.ReadByte();
					if ((opcode1 & 0b11110000) == 0)
						litCount = literalCount(opcode1, src) + 3;
				}

				if (litCount > 0U)
					opcode1 = copy(litCount, src, dst);
			}
		}

		private static byte copy(int count, Stream src, Stream dst)
		{
			for (int i = 0; i < count; ++i)
			{
				byte b = (byte)src.ReadByte();
				dst.WriteByte(b);
			}
			return (byte)src.ReadByte();
		}

		private static int literalCount(int code, Stream src)
		{
			int lowbits = code & 0b1111;
			if (lowbits == 0)
			{
				byte lastByte;
				for (lastByte = (byte)src.ReadByte(); lastByte == 0; lastByte = (byte)src.ReadByte())
					lowbits += byte.MaxValue;
				lowbits += 0xF + lastByte;
			}
			return lowbits;
		}

		private static int readCompressedBytes(int opcode1, int validBits, Stream compressed)
		{
			int compressedBytes = opcode1 & validBits;
			if (compressedBytes == 0)
			{
				byte lastByte;
				for (lastByte = (byte)compressed.ReadByte(); lastByte == 0; lastByte = (byte)compressed.ReadByte())
					compressedBytes += byte.MaxValue;
				compressedBytes += lastByte + validBits;
			}
			return compressedBytes + 2;
		}

		private static int twoByteOffset(ref int offset, int addedValue, Stream stream)
		{
			int firstByte = stream.ReadByte();
			offset |= firstByte >> 2;
			offset |= stream.ReadByte() << 6;
			offset += addedValue;
			return firstByte;
		}

		#endregion
	}
}
