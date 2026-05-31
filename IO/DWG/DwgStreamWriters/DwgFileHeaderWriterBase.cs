using CSUtilities.Converters;
using CSUtilities.Text;
using System;
using System.IO;
using System.Text;

namespace ACadSharp.IO.DWG
{
	internal abstract class DwgFileHeaderWriterBase : IDwgFileHeaderWriter
	{
		public abstract int HandleSectionOffset { get; }

		protected abstract int _fileHeaderSize { get; }

		protected DwgFileHeader _fileHeader { get; }

		protected ACadVersion _version;

		protected Encoding _encoding;

		protected Stream _stream;

		protected CadDocument _document;

		public DwgFileHeaderWriterBase(Stream stream, Encoding encoding, CadDocument model)
		{
			if (!stream.CanSeek || !stream.CanWrite)
			{
				throw new ArgumentException();
			}

			this._document = model;
			this._stream = stream;
			this._version = model.Header.Version;
			this._encoding = encoding;
		}

		public abstract void AddSection(string name, MemoryStream stream, bool isCompressed, int decompsize = 0x7400);

		public abstract void WriteFile();

		protected ushort getFileCodePage()
		{
			ushort codePage = (ushort)CadUtils.GetCodeIndex(CadUtils.GetCodePage(_document.Header.CodePage));
			if (codePage < 1)
			{
				return 30;
			}
			else
			{
				return codePage;
			}
		}

		protected void applyMask(byte[] buffer, int offset, int length)
		{
			int mask = 0x4164536B ^ (int)this._stream.Position;
			int diff = offset + length;
			while (offset < diff)
			{
				buffer[offset++] ^= (byte)mask;
				if (offset >= diff)
					break;
				buffer[offset++] ^= (byte)(mask >> 8);
				if (offset >= diff)
					break;
				buffer[offset++] ^= (byte)(mask >> 16);
				if (offset >= diff)
					break;
				buffer[offset++] ^= (byte)(mask >> 24);
			}
		}

		protected static void writeRawInt32(Stream stream, int value)
		{
			stream.WriteByte((byte)value);
			stream.WriteByte((byte)(value >> 8));
			stream.WriteByte((byte)(value >> 16));
			stream.WriteByte((byte)(value >> 24));
		}

		protected static void writeRawUInt32(Stream stream, uint value)
		{
			writeRawInt32(stream, (int)value);
		}

		protected static void writeRawInt64(Stream stream, long value)
		{
			writeRawUInt64(stream, (ulong)value);
		}

		protected static void writeRawUInt64(Stream stream, ulong value)
		{
			stream.WriteByte((byte)value);
			stream.WriteByte((byte)(value >> 8));
			stream.WriteByte((byte)(value >> 16));
			stream.WriteByte((byte)(value >> 24));
			stream.WriteByte((byte)(value >> 32));
			stream.WriteByte((byte)(value >> 40));
			stream.WriteByte((byte)(value >> 48));
			stream.WriteByte((byte)(value >> 56));
		}

		protected static void writeAsciiBytes(Stream stream, string value, int length)
		{
			int i = 0;
			for (; i < length && i < value.Length; i++)
			{
				stream.WriteByte((byte)value[i]);
			}

			writeZeroes(stream, length - i);
		}

		protected static void writeZeroes(Stream stream, int count)
		{
			for (int i = 0; i < count; i++)
			{
				stream.WriteByte(0);
			}
		}

		protected bool checkEmptyBytes(byte[] buffer, ulong offset, ulong spearBytes)
		{
			bool result = true;
			ulong num = offset + spearBytes;

			for (ulong i = offset; i < num; i++)
			{
				if (buffer[i] != 0)
				{
					result = false;
					break;
				}
			}

			return result;
		}

		protected void writeMagicNumber()
		{
			for (int i = 0; i < (int)(this._stream.Position % 0x20); i++)
			{
				this._stream.WriteByte(DwgCheckSumCalculator.MagicSequence[i]);
			}
		}

		protected void applyMagicSequence(MemoryStream stream)
		{
			byte[] buffer = stream.GetBuffer();
			for (int i = 0; i < (int)stream.Length; i++)
			{
				buffer[i] ^= DwgCheckSumCalculator.MagicSequence[i];
			}
		}
	}
}
