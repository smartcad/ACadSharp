using System.Buffers;
using System.IO;
using System.Text;

namespace ACadSharp.IO.DWG
{
	internal class DwgStreamWriterAC21 : DwgStreamWriterAC18
	{
		public DwgStreamWriterAC21(Stream stream, Encoding encoding) : base(stream, encoding)
		{
		}

		public override void WriteVariableText(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				base.WriteBitShort(0);
				return;
			}
			base.WriteBitShort((short)value.Length);
			int byteCount = Encoding.Unicode.GetByteCount(value);
			byte[] bytes = ArrayPool<byte>.Shared.Rent(byteCount);
			try
			{
				Encoding.Unicode.GetBytes(value, 0, value.Length, bytes, 0);
				base.WriteBytes(bytes, 0, byteCount);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(bytes);
			}
		}

		public override void WriteTextUnicode(string value)
		{
			value = string.IsNullOrEmpty(value) ? string.Empty : value;
			this.WriteRawShort((short)(value.Length + 1));
			int byteCount = Encoding.Unicode.GetByteCount(value);
			if (byteCount > 0)
			{
				byte[] bytes = ArrayPool<byte>.Shared.Rent(byteCount);
				try
				{
					Encoding.Unicode.GetBytes(value, 0, value.Length, bytes, 0);
					this.WriteBytes(bytes, 0, byteCount);
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(bytes);
				}
			}
			base.Stream.WriteByte(0);
			base.Stream.WriteByte(0);
		}
	}
}
