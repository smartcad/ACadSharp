using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ACadSharp.IO.DXF
{
	internal class DxfTextReader : DxfStreamReaderBase
	{
		protected override Stream baseStream { get { return this._stream.BaseStream; } }

		private StreamReader _stream;
		private char[] _lineBuffer = new char[512];
		private int _lineLength = 0;

		public DxfTextReader(Stream stream, Encoding encoding)
		{
			this._stream = new StreamReader(stream, encoding);
			this.Start();
		}

		public override void Start()
		{
			base.Start();
			this._lineLength = 0;
			this._stream.DiscardBufferedData();
		}

		public override void ReadNext()
		{
			base.ReadNext();
			this.Position += 2;
		}

		/// <summary>
		/// Reads a line from the stream into the reusable character buffer.
		/// Returns the length of the read line.
		/// </summary>
		private int readLineToBuffer()
		{
			_lineLength = 0;
			while (true)
			{
				int next = this._stream.Read();
				if (next == -1)
				{
					break;
				}
				char c = (char)next;
				if (c == '\r')
				{
					int peek = this._stream.Peek();
					if (peek == '\n')
					{
						this._stream.Read();
					}
					break;
				}
				if (c == '\n')
				{
					break;
				}
				
				if (_lineLength >= _lineBuffer.Length)
				{
					char[] temp = new char[_lineBuffer.Length * 2];
					System.Array.Copy(_lineBuffer, 0, temp, 0, _lineLength);
					_lineBuffer = temp;
				}
				_lineBuffer[_lineLength++] = c;
			}
			return _lineLength;
		}

		protected override string readStringLine()
		{
			this.readLineToBuffer();
			this.ValueRaw = new string(this._lineBuffer, 0, this._lineLength);
			return this.ValueRaw;
		}

		protected override DxfCode readCode()
		{
			this.readLineToBuffer();
			var span = new ReadOnlySpan<char>(this._lineBuffer, 0, this._lineLength);

			if (TryParseInt(span, out int value))
			{
				return (DxfCode)value;
			}

			this.Position++;

			return DxfCode.Invalid;
		}

		protected override bool lineAsBool()
		{
			this.readLineToBuffer();
			var span = new ReadOnlySpan<char>(this._lineBuffer, 0, this._lineLength);

			if (TryParseInt(span, out int result))
			{
				return result > 0;
			}

			return false;
		}

		protected override double lineAsDouble()
		{
			this.readLineToBuffer();
			var str = new string(this._lineBuffer, 0, this._lineLength);

			if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
			{
				return result;
			}

			return 0.0;
		}

		protected override short lineAsShort()
		{
			this.readLineToBuffer();
			var span = new ReadOnlySpan<char>(this._lineBuffer, 0, this._lineLength);

			if (TryParseInt(span, out int result))
			{
				return (short)result;
			}

			return 0;
		}

		protected override int lineAsInt()
		{
			this.readLineToBuffer();
			var span = new ReadOnlySpan<char>(this._lineBuffer, 0, this._lineLength);

			if (TryParseInt(span, out int result))
			{
				return result;
			}

			return 0;
		}

		protected override long lineAsLong()
		{
			this.readLineToBuffer();
			var span = new ReadOnlySpan<char>(this._lineBuffer, 0, this._lineLength);

			if (TryParseLong(span, out long result))
			{
				return result;
			}

			return 0;
		}

		protected override ulong lineAsHandle()
		{
			this.readLineToBuffer();
			var span = new ReadOnlySpan<char>(this._lineBuffer, 0, this._lineLength);

			if (TryParseHex(span, out ulong result))
			{
				return result;
			}

			return 0;
		}

		protected override byte[] lineAsBinaryChunk()
		{
			this.readLineToBuffer();
			
			int len = this._lineLength;
			while (len > 0 && char.IsWhiteSpace(this._lineBuffer[len - 1]))
			{
				len--;
			}
			
			int byteCount = len / 2;
			byte[] bytes = new byte[byteCount];
			
			for (int i = 0; i < byteCount; i++)
			{
				char c1 = this._lineBuffer[i * 2];
				char c2 = this._lineBuffer[i * 2 + 1];
				
				int d1 = parseHexChar(c1);
				int d2 = parseHexChar(c2);
				if (d1 < 0 || d2 < 0)
				{
					return Array.Empty<byte>();
				}
				bytes[i] = (byte)((d1 << 4) | d2);
			}
			
			return bytes;
		}

		private static int parseHexChar(char c)
		{
			if (c >= '0' && c <= '9') return c - '0';
			if (c >= 'a' && c <= 'f') return c - 'a' + 10;
			if (c >= 'A' && c <= 'F') return c - 'A' + 10;
			return -1;
		}

		#region Zero-Allocation Parsers

		private static bool TryParseInt(ReadOnlySpan<char> span, out int value)
		{
			value = 0;
			if (span.IsEmpty) return false;
			int i = 0;
			while (i < span.Length && char.IsWhiteSpace(span[i])) i++;
			if (i == span.Length) return false;

			bool negative = false;
			if (span[i] == '-')
			{
				negative = true;
				i++;
			}
			else if (span[i] == '+')
			{
				i++;
			}

			int temp = 0;
			bool hasDigits = false;
			while (i < span.Length && span[i] >= '0' && span[i] <= '9')
			{
				temp = temp * 10 + (span[i] - '0');
				i++;
				hasDigits = true;
			}
			if (!hasDigits) return false;

			while (i < span.Length && char.IsWhiteSpace(span[i])) i++;
			if (i != span.Length) return false;

			value = negative ? -temp : temp;
			return true;
		}

		private static bool TryParseLong(ReadOnlySpan<char> span, out long value)
		{
			value = 0;
			if (span.IsEmpty) return false;
			int i = 0;
			while (i < span.Length && char.IsWhiteSpace(span[i])) i++;
			if (i == span.Length) return false;

			bool negative = false;
			if (span[i] == '-')
			{
				negative = true;
				i++;
			}
			else if (span[i] == '+')
			{
				i++;
			}

			long temp = 0;
			bool hasDigits = false;
			while (i < span.Length && span[i] >= '0' && span[i] <= '9')
			{
				temp = temp * 10 + (span[i] - '0');
				i++;
				hasDigits = true;
			}
			if (!hasDigits) return false;

			while (i < span.Length && char.IsWhiteSpace(span[i])) i++;
			if (i != span.Length) return false;

			value = negative ? -temp : temp;
			return true;
		}

		private static bool TryParseHex(ReadOnlySpan<char> span, out ulong value)
		{
			value = 0;
			if (span.IsEmpty) return false;
			int i = 0;
			while (i < span.Length && char.IsWhiteSpace(span[i])) i++;
			if (i == span.Length) return false;

			ulong temp = 0;
			bool hasDigits = false;
			while (i < span.Length)
			{
				char c = span[i];
				int digit;
				if (c >= '0' && c <= '9') digit = c - '0';
				else if (c >= 'a' && c <= 'f') digit = c - 'a' + 10;
				else if (c >= 'A' && c <= 'F') digit = c - 'A' + 10;
				else break;

				temp = (temp << 4) | (uint)digit;
				i++;
				hasDigits = true;
			}
			if (!hasDigits) return false;

			while (i < span.Length && char.IsWhiteSpace(span[i])) i++;
			if (i != span.Length) return false;

			value = temp;
			return true;
		}

		#endregion
	}
}
