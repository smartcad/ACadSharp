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
			MemoryStream memoryStream = new MemoryStream(new byte[decompressedSize]);
			DecompressToDest(compressed, memoryStream);
			memoryStream.Position = 0L;
			return memoryStream;
		}

		/// <summary>
		/// Decompress a compressed source stream into a destination stream.
		/// Uses direct buffer access when both streams are MemoryStreams.
		/// </summary>
		public static void DecompressToDest(Stream src, Stream dst)
		{
			// Fast path: if src is a MemoryStream with accessible buffer, use array-based decompression
			if (src is MemoryStream srcMs && srcMs.TryGetBuffer(out ArraySegment<byte> srcSeg)
				&& dst is MemoryStream dstMs && dstMs.TryGetBuffer(out ArraySegment<byte> dstSeg))
			{
				int si = srcSeg.Offset + (int)srcMs.Position;
				int dstStart = dstSeg.Offset + (int)dstMs.Position;
				int written = DecompressBufferToBuffer(srcSeg.Array, ref si, dstSeg.Array, dstStart);
				srcMs.Position = si - srcSeg.Offset;
				dstMs.Position += written;
				return;
			}

			// Fallback: original stream-based implementation
			DecompressToDestStream(src, dst);
		}

		/// <summary>
		/// Buffer-to-buffer decompression. Returns number of bytes written to dst.
		/// </summary>
		internal static int DecompressBufferToBuffer(byte[] src, ref int si, byte[] dst, int dstOffset)
		{
			int di = dstOffset;

			int opcode1 = src[si++];

			if ((opcode1 & 0xF0) == 0)
			{
				int litCount = literalCountBuf(opcode1, src, ref si) + 3;
				Buffer.BlockCopy(src, si, dst, di, litCount);
				si += litCount;
				di += litCount;
				opcode1 = src[si++];
			}

			while (opcode1 != 0x11)
			{
				int compOffset = 0;
				int compressedBytes = 0;

				if (opcode1 < 0x10 || opcode1 >= 0x40)
				{
					compressedBytes = (opcode1 >> 4) - 1;
					byte opcode2 = src[si++];
					compOffset = ((opcode1 >> 2 & 3) | (opcode2 << 2)) + 1;
				}
				else if (opcode1 < 0x20)
				{
					compressedBytes = readCompressedBytesBuf(opcode1, 0b0111, src, ref si);
					compOffset = (opcode1 & 8) << 11;
					opcode1 = twoByteOffsetBuf(ref compOffset, 0x4000, src, ref si);
				}
				else
				{
					compressedBytes = readCompressedBytesBuf(opcode1, 0b00011111, src, ref si);
					opcode1 = twoByteOffsetBuf(ref compOffset, 1, src, ref si);
				}

				// Copy back-reference from already-decompressed output (always byte-by-byte for overlap safety)
				int copyFrom = di - compOffset;
				for (int end = di + compressedBytes; di < end; di++, copyFrom++)
				{
					dst[di] = dst[copyFrom];
				}

				// Literal count from low 2 bits
				int litCount = opcode1 & 3;
				if (litCount == 0)
				{
					opcode1 = src[si++];
					if ((opcode1 & 0xF0) == 0)
						litCount = literalCountBuf(opcode1, src, ref si) + 3;
				}

				if (litCount > 0)
				{
					Buffer.BlockCopy(src, si, dst, di, litCount);
					si += litCount;
					di += litCount;
					opcode1 = src[si++];
				}
			}

			return di - dstOffset;
		}

		private static int literalCountBuf(int code, byte[] src, ref int si)
		{
			int lowbits = code & 0xF;
			if (lowbits == 0)
			{
				byte lastByte;
				for (lastByte = src[si++]; lastByte == 0; lastByte = src[si++])
					lowbits += 0xFF;
				lowbits += 0xF + lastByte;
			}
			return lowbits;
		}

		private static int readCompressedBytesBuf(int opcode1, int validBits, byte[] src, ref int si)
		{
			int compressedBytes = opcode1 & validBits;
			if (compressedBytes == 0)
			{
				byte lastByte;
				for (lastByte = src[si++]; lastByte == 0; lastByte = src[si++])
					compressedBytes += 0xFF;
				compressedBytes += lastByte + validBits;
			}
			return compressedBytes + 2;
		}

		private static int twoByteOffsetBuf(ref int offset, int addedValue, byte[] src, ref int si)
		{
			int firstByte = src[si++];
			offset |= firstByte >> 2;
			offset |= src[si++] << 6;
			offset += addedValue;
			return firstByte;
		}

		/// <summary>
		/// Original stream-based decompression (fallback for non-MemoryStream).
		/// </summary>
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
	}
}
