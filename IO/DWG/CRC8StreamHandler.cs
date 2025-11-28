using System.IO;
using System.Runtime.CompilerServices;

namespace ACadSharp.IO.DWG
{
	/// <summary>
	/// The DWG file format uses a modification of a standard cyclic redundancy check as an error detecting mechanism. <br/>
	/// This class checks the integrity of the file using this check.
	/// </summary>
	/// <remarks>
	/// This method is used extensively in pre-R13 files, but seems only to be used in the header for R13 and beyond.
	/// </remarks>
	internal class CRC8StreamHandler : Stream
	{
		public override bool CanRead => this._stream.CanRead;

		public override bool CanSeek => this._stream.CanSeek;

		public override bool CanWrite => this._stream.CanWrite;

		public override long Length => this._stream.Length;

		public override long Position
		{
			get => this._stream.Position;
			set => this._stream.Position = value;
		}

		public ushort Seed { get; private set; }

		private Stream _stream;

		public CRC8StreamHandler(Stream stream, ushort seed)
		{
			this._stream = stream;
			this.Seed = seed;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int nbytes = this._stream.Read(buffer, offset, count);
			int length = offset + nbytes; // Use actual bytes read, not requested count
			ushort seed = this.Seed;
			ushort[] crcTable = CRC.CrcTable;

			// Inline the CRC calculation to avoid method call overhead per byte
			for (int index = offset; index < length; ++index)
			{
				int tableIndex = buffer[index] ^ (byte)seed;
				seed = (ushort)((uint)seed >> 8 ^ crcTable[tableIndex]);
			}

			this.Seed = seed;
			return nbytes;
		}

		public override void Flush() => this._stream.Flush();

		public override long Seek(long offset, SeekOrigin origin) => this._stream.Seek(offset, origin);

		public override void SetLength(long value) => this._stream.SetLength(value);

		public override void Write(byte[] buffer, int offset, int count)
		{
			int length = offset + count;

			for (int index = offset; index < length; ++index)
				this.Seed = decode(this.Seed, buffer[index]);

			this._stream.Write(buffer, offset, count);
		}

		public static ushort GetCRCValue(ushort seed, byte[] buffer, long startPos, long endPos)
		{
			ushort currValue = seed;
			int index = (int)startPos;
			ushort[] crcTable = CRC.CrcTable;

			while (endPos-- > 0)
			{
				int tableIndex = buffer[index] ^ (byte)currValue;
				currValue = (ushort)((uint)currValue >> 8 ^ crcTable[tableIndex]);
				index++;
			}

			return currValue;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ushort decode(ushort key, byte value)
		{
			int index = value ^ (byte)key;
			key = (ushort)((uint)key >> 8 ^ CRC.CrcTable[index]);
			return key;
		}
	}
}
