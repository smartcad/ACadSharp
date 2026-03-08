using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ACadSharp.IO.DXF
{
	internal class DxfBinaryReader : DxfStreamReaderBase
	{
		public const string Sentinel = "AutoCAD Binary DXF\r\n\u001a\0";

		public override int Position { get { return (int)this.baseStream.Position; } }

		protected override Stream baseStream { get { return this._stream.BaseStream; } }

		protected BinaryReader _stream;

		private Encoding _encoding;

		public DxfBinaryReader(Stream stream) : this(stream, Encoding.ASCII) { }

		public DxfBinaryReader(Stream stream, Encoding encoding)
		{
			this._encoding = encoding;
			this._stream = new BinaryReader(stream, this._encoding);

			this.Start();
		}

		public override void Start()
		{
			base.Start();

			byte[] sentinel = this._stream.ReadBytes(22);
			//AutoCAD Binary DXF\r\n\u001a\0
			string s = Encoding.ASCII.GetString(sentinel);
		}

		protected override string readStringLine()
		{
			byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(256);
			int count = 0;

			byte b = this._stream.ReadByte();
			while (b != 0)
			{
				if (count == buffer.Length)
				{
					byte[] larger = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
					System.Buffer.BlockCopy(buffer, 0, larger, 0, count);
					System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
					buffer = larger;
				}
				buffer[count++] = b;
				b = this._stream.ReadByte();
			}

			this.ValueRaw = this._encoding.GetString(buffer, 0, count);
			System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
			return this.ValueRaw;
		}

		protected override DxfCode readCode()
		{
			return (DxfCode)this._stream.ReadInt16();
		}

		protected override bool lineAsBool()
		{
			return this._stream.ReadByte() > 0;
		}

		protected override double lineAsDouble()
		{
			return this._stream.ReadDouble();
		}

		protected override short lineAsShort()
		{
			return this._stream.ReadInt16();
		}

		protected override int lineAsInt()
		{
			return this._stream.ReadInt32();
		}

		protected override long lineAsLong()
		{
			return this._stream.ReadInt64();
		}

		protected override ulong lineAsHandle()
		{
			var str = this.readStringLine();

			if (ulong.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong result))
			{
				return result;
			}

			return 0;
		}

		protected override byte[] lineAsBinaryChunk()
		{
			byte length = this._stream.ReadByte();
			return this._stream.ReadBytes(length);
		}
	}
}
