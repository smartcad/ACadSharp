using ACadSharp.Exceptions;
using System;
using System.IO;
using System.Text;

namespace ACadSharp.IO.DXF
{
	internal abstract class DxfStreamReaderBase : IDxfStreamReader
	{
		public DxfCode DxfCode { get; protected set; }

		public GroupCodeValueType GroupCodeValue { get; protected set; }

		public int Code { get { return (int)this.DxfCode; } }

		public object Value { get; protected set; }

		public virtual int Position { get; protected set; }

		public string ValueRaw { get; protected set; }

		protected string _stringValue;
		protected double _doubleValue;
		protected short _shortValue;
		protected int _intValue;
		protected long _longValue;
		protected ulong _handleValue;
		protected bool _boolValue;
		protected byte[] _binaryValue;

		public string ValueAsString
		{
			get
			{
				if (this.GroupCodeValue == GroupCodeValueType.String || 
					this.GroupCodeValue == GroupCodeValueType.Comment || 
					this.GroupCodeValue == GroupCodeValueType.ExtendedDataString)
				{
					return unescapeString(this._stringValue);
				}
				return this.Value?.ToString() ?? string.Empty;
			}
		}

		public bool ValueAsBool { get { return this._boolValue; } }

		public short ValueAsShort { get { return this._shortValue; } }

		public ushort ValueAsUShort { get { return (ushort)this._shortValue; } }

		public int ValueAsInt { get { return this._intValue; } }

		public long ValueAsLong { get { return this._longValue; } }

		public double ValueAsDouble { get { return this._doubleValue; } }

		public double ValueAsAngle { get { return this._doubleValue * MathUtils.RadToDegFactor; } }

		public ulong ValueAsHandle { get { return this._handleValue; } }

		public byte[] ValueAsBinaryChunk { get { return this._binaryValue; } }

		protected abstract Stream baseStream { get; }

		public virtual void ReadNext()
		{
			this.DxfCode = this.readCode();
			this.GroupCodeValue = ACadSharp.GroupCodeValue.TransformValue(this.Code);
			this.readAndStoreValue(this.GroupCodeValue);
		}

		public bool Find(string dxfEntry)
		{
			this.Start();

			do
			{
				this.ReadNext();
			}
			while (this.ValueAsString != dxfEntry && (this.ValueAsString != DxfFileToken.EndOfFile));

			return this.ValueAsString == dxfEntry;
		}

		public override string ToString()
		{
			return $"{Code} | {Value}";
		}

		public virtual void Start()
		{
			this.DxfCode = DxfCode.Invalid;
			this.Value = string.Empty;
			this._stringValue = null;
			this._doubleValue = 0;
			this._shortValue = 0;
			this._intValue = 0;
			this._longValue = 0;
			this._handleValue = 0;
			this._boolValue = false;
			this._binaryValue = null;

			this.baseStream.Position = 0;

			this.Position = 0;
		}

		protected abstract DxfCode readCode();

		protected abstract string readStringLine();

		protected abstract double lineAsDouble();

		protected abstract short lineAsShort();

		protected abstract int lineAsInt();

		protected abstract long lineAsLong();

		protected abstract ulong lineAsHandle();

		protected abstract byte[] lineAsBinaryChunk();

		protected abstract bool lineAsBool();

		private void readAndStoreValue(GroupCodeValueType code)
		{
			switch (code)
			{
				case GroupCodeValueType.String:
				case GroupCodeValueType.Comment:
				case GroupCodeValueType.ExtendedDataString:
					this._stringValue = this.readStringLine();
					this.Value = this._stringValue;
					break;
				case GroupCodeValueType.Point3D:
				case GroupCodeValueType.Double:
				case GroupCodeValueType.ExtendedDataDouble:
					this._doubleValue = this.lineAsDouble();
					this.Value = this._doubleValue;
					break;
				case GroupCodeValueType.Byte:
				case GroupCodeValueType.Int16:
				case GroupCodeValueType.ExtendedDataInt16:
					this._shortValue = this.lineAsShort();
					this.Value = this._shortValue;
					break;
				case GroupCodeValueType.Int32:
				case GroupCodeValueType.ExtendedDataInt32:
					this._intValue = this.lineAsInt();
					this.Value = this._intValue;
					break;
				case GroupCodeValueType.Int64:
					this._longValue = this.lineAsLong();
					this.Value = this._longValue;
					break;
				case GroupCodeValueType.Handle:
				case GroupCodeValueType.ObjectId:
				case GroupCodeValueType.ExtendedDataHandle:
					this._handleValue = this.lineAsHandle();
					this.Value = this._handleValue;
					break;
				case GroupCodeValueType.Bool:
					this._boolValue = this.lineAsBool();
					this.Value = this._boolValue;
					break;
				case GroupCodeValueType.Chunk:
				case GroupCodeValueType.ExtendedDataChunk:
					this._binaryValue = this.lineAsBinaryChunk();
					this.Value = this._binaryValue;
					break;
				case GroupCodeValueType.None:
				default:
					throw new DxfException((int)code, this.Position);
			}
		}

		private static string unescapeString(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			StringBuilder builder = null;
			for (int i = 0; i < value.Length; i++)
			{
				if (value[i] != '^' || i + 1 >= value.Length)
				{
					builder?.Append(value[i]);
					continue;
				}

				char replacement;
				switch (value[i + 1])
				{
					case 'J':
						replacement = '\n';
						break;
					case 'M':
						replacement = '\r';
						break;
					case 'I':
						replacement = '\t';
						break;
					case ' ':
						replacement = '^';
						break;
					default:
						builder?.Append(value[i]);
						continue;
				}

				if (builder == null)
				{
					builder = new StringBuilder(value.Length);
					builder.Append(value, 0, i);
				}

				builder.Append(replacement);
				i++;
			}

			return builder?.ToString() ?? value;
		}
	}
}
