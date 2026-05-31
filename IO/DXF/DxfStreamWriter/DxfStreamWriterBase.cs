using System;
using System.Text;

namespace ACadSharp.IO.DXF
{
	internal abstract class DxfStreamWriterBase : IDxfStreamWriter
	{
		public bool WriteOptional { get; } = false;

		public void Write(DxfCode code, object value)
		{
			this.Write((int)code, value, null);
		}

		public void Write(DxfCode code, object value, DxfClassMap map)
		{
			this.Write((int)code, value, map);
		}

		public void Write(int code, object value)
		{
			this.Write(code, value, null);
		}

		public void Write(int code, CSMath.IVector value, DxfClassMap map)
		{
			for (int i = 0; i < value.Dimension; i++)
			{
				this.Write(code + i * 10, value[i], map);
			}
		}

		public void WriteCmColor(int code, Color color, DxfClassMap map = null)
		{
			if (GroupCodeValue.TransformValue(code) == GroupCodeValueType.Int16)
			{
				//BS: Color Index
				this.Write(code, Convert.ToInt16(color.GetApproxIndex()));
			}
			else
			{
				uint rgb;

				if (color.IsTrueColor)
				{
					rgb = (uint)color.B | ((uint)color.G << 8) | ((uint)color.R << 16) | (0b1100_0010u << 24);
				}
				else
				{
					rgb = (uint)(byte)color.Index | (0b1100_0001u << 24);
				}

				//BL: RGB value
				this.Write(code, unchecked((int)rgb), map);
			}
		}

		public void WriteHandle(int code, IHandledCadObject value, DxfClassMap map)
		{
			if (value != null)
			{
				this.Write(code, value.Handle, map);
			}
		}

		public void WriteName(int code, INamedCadObject value, DxfClassMap map)
		{
			if (value != null)
			{
				this.Write(code, value.Name, map);
			}
		}

		public void Write(int code, object value, DxfClassMap map)
		{
			if (value == null)
			{
				return;
			}

			if (map != null && map.DxfProperties.TryGetValue(code, out DxfProperty prop))
			{
				if (prop.ReferenceType.HasFlag(DxfReferenceType.Optional) && !WriteOptional)
				{
					return;
				}

				if (prop.ReferenceType.HasFlag(DxfReferenceType.IsAngle))
				{
					value = (double)value * MathUtils.RadToDegFactor;
				}
			}

			this.writeDxfCode(code);

			if (value is string s)
			{
				this.writeValue(code, escapeString(s));
			}
			else
			{
				this.writeValue(code, value);
			}
		}

		/// <inheritdoc/>
		public abstract void Dispose();

		public abstract void Flush();

		public abstract void Close();

		protected abstract void writeDxfCode(int code);

		protected abstract void writeValue(int code, object value);

		private static string escapeString(string value)
		{
			StringBuilder builder = null;

			for (int i = 0; i < value.Length; i++)
			{
				string replacement = null;
				switch (value[i])
				{
					case '^':
						replacement = "^ ";
						break;
					case '\n':
						replacement = "^J";
						break;
					case '\r':
						replacement = "^M";
						break;
					case '\t':
						replacement = "^I";
						break;
				}

				if (replacement == null)
				{
					builder?.Append(value[i]);
					continue;
				}

				if (builder == null)
				{
					builder = new StringBuilder(value.Length + 2);
					builder.Append(value, 0, i);
				}

				builder.Append(replacement);
			}

			return builder?.ToString() ?? value;
		}
	}
}
