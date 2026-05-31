using ACadSharp.Objects;
using System.IO;
using System.Text;

namespace ACadSharp.IO.DWG
{
	internal class DwgStreamWriterAC18 : DwgStreamWriterAC15
	{
		public DwgStreamWriterAC18(Stream stream, Encoding encoding) : base(stream, encoding)
		{
		}

		public override void WriteCmColor(Color value, BookColor bookColor)
		{
			//CMC:
			//BS: color index(always 0)
			this.WriteBitShort(0);

			uint rgb;

			if (value.IsTrueColor)
			{
				rgb = (uint)value.B | ((uint)value.G << 8) | ((uint)value.R << 16) | (0b1100_0010u << 24);
			}
			else if (value.IsByLayer)
			{
				rgb = (uint)(byte)value.Index | (0b1100_0000u << 24);
			}
			else
			{
				rgb = (uint)(byte)value.Index | (0b1100_0011u << 24);
			}

			//BL: RGB value
			this.WriteBitLong(unchecked((int)rgb));


			//RC: Color Byte
			if(bookColor != null)
			{
				this.WriteByte(3);
				this.WriteVariableText(bookColor.ColorName);
				this.WriteVariableText(bookColor.BookName);
			}
			else
				this.WriteByte(0);

			//(&1 => color name follows(TV),
			//&2 => book name follows(TV))
		}

		public override void WriteEnColor(Color color, Transparency transparency, bool isBookColor)
		{
			//BS : color number: flags + color index
			ushort size = 0;

			if (color.IsByBlock && transparency.IsByLayer && !isBookColor)
			{
				base.WriteBitShort(0);
				return;
			}

			//0x2000: color is followed by a transparency BL
			if (!transparency.IsByLayer)
			{
				size = (ushort)(size | 0b10000000000000);
			}

			//0x4000: has AcDbColor reference (0x8000 is also set in this case).
			if (isBookColor)
			{
				size = (ushort)(size | 0x4000);
				size = (ushort)(size | 0x8000);
			}

			//0x8000: complex color (rgb).
			else if (color.IsTrueColor)
			{
				size = (ushort)(size | 0x8000);
			}
			else
			{
				//Color index: if no flags were set, the color is looked up by the color number (ACI color).
				size = (ushort)(size | (ushort)color.Index);
			}

			base.WriteBitShort((short)size);

			if (color.IsTrueColor)
			{
				uint rgb = (uint)color.B | ((uint)color.G << 8) | ((uint)color.R << 16) | (0b1100_0010u << 24);
				base.WriteBitLong(unchecked((int)rgb));
			}

			if (!transparency.IsByLayer)
			{
				//The first byte represents the transparency type:
				//0 = BYLAYER,
				//1 = BYBLOCK,
				//3 = the transparency value in the last byte.
				base.WriteBitLong(Transparency.ToAlphaValue(transparency));
			}
		}
	}
}
