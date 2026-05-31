using System;
using System.IO;

namespace ACadSharp.IO.DXF
{
	internal class DxfAsciiWriter : DxfStreamWriterBase
	{
		private static readonly char[] _hexChars = "0123456789ABCDEF".ToCharArray();

		private TextWriter _stream;

		public DxfAsciiWriter(StreamWriter stream)
		{
			this._stream = stream;
		}

		public override void Dispose()
		{
			//this._stream.Dispose();
		}

		public override void Flush()
		{
			this._stream.Flush();
		}

		public override void Close()
		{
			this._stream.Close();
		}

		protected override void writeDxfCode(int code)
		{
			if (code < 10)
			{
				this._stream.WriteLine("  {0}", code.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
			else if (code < 100)
			{
				this._stream.WriteLine(" {0}", code.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
			else
			{
				this._stream.WriteLine(code.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
		}

		protected override void writeValue(int code, object value)
		{
			GroupCodeValueType groupCode = GroupCodeValue.TransformValue(code);

			switch (groupCode)
			{
				case GroupCodeValueType.None:
					break;
				case GroupCodeValueType.String:
				case GroupCodeValueType.Comment:
				case GroupCodeValueType.ExtendedDataString:
					this._stream.WriteLine(value.ToString());
					return;
				case GroupCodeValueType.Point3D:
				case GroupCodeValueType.Double:
				case GroupCodeValueType.ExtendedDataDouble:
					this._stream.WriteLine(Convert.ToDouble(value).ToString("0.0###############", System.Globalization.CultureInfo.InvariantCulture));
					return;
				case GroupCodeValueType.Byte:
				case GroupCodeValueType.Int16:
				case GroupCodeValueType.ExtendedDataInt16:
					this._stream.WriteLine(Convert.ToInt16(value).ToString(System.Globalization.CultureInfo.InvariantCulture));
					return;
				case GroupCodeValueType.Int32:
				case GroupCodeValueType.ExtendedDataInt32:
					this._stream.WriteLine(Convert.ToInt32(value).ToString(System.Globalization.CultureInfo.InvariantCulture));
					return;
				case GroupCodeValueType.Int64:
					this._stream.WriteLine(Convert.ToInt64(value).ToString(System.Globalization.CultureInfo.InvariantCulture));
					return;
				case GroupCodeValueType.Handle:
				case GroupCodeValueType.ObjectId:
				case GroupCodeValueType.ExtendedDataHandle:
					this._stream.WriteLine(((ulong)value).ToString("X", System.Globalization.CultureInfo.InvariantCulture));
					return;
				case GroupCodeValueType.Bool:
					this._stream.WriteLine(Convert.ToInt16(value).ToString(System.Globalization.CultureInfo.InvariantCulture));
					return;
				case GroupCodeValueType.Chunk:
				case GroupCodeValueType.ExtendedDataChunk:
					byte[] arr = value as byte[];
					char[] chars = new char[arr.Length * 2];
					for (int i = 0, j = 0; i < arr.Length; i++)
					{
						byte v = arr[i];
						chars[j++] = _hexChars[v >> 4];
						chars[j++] = _hexChars[v & 0xF];
					}
					this._stream.Write(chars);
					this._stream.Write(Environment.NewLine);
					return;
			}

			this._stream.WriteLine(value.ToString());
		}
	}
}
