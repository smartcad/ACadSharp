using ACadSharp.Exceptions;
using CSMath;
using CSUtilities.Converters;
using CSUtilities.IO;
using CSUtilities.Text;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ACadSharp.IO.DWG
{
	internal abstract class DwgStreamReaderBase : StreamIO, IDwgStreamReader
	{
        internal static byte[] ByteArray16 = new byte[16];
        internal static byte[] ByteArray8 = new byte[8];
        internal static byte[] ByteArray4 = new byte[4];
		internal static byte[] ByteArray2 = new byte[2];

		// Direct byte buffer for bypassing virtual Stream.ReadByte() dispatch.
		// On Mono runtime (MacCatalyst), virtual dispatch per byte is extremely slow.
		private readonly byte[] _buf;
		private int _bufPos;
		private readonly int _bufEnd;
		private readonly bool _hasBuf;

		/// <inheritdoc/>
		public int BitShift { get; set; }

		/// <inheritdoc/>
		public override long Position
		{
			get => _hasBuf ? _bufPos : this._stream.Position;
			set
			{
				if (_hasBuf)
					_bufPos = (int)value;
				else
					this._stream.Position = value;
				this.BitShift = 0;
			}
		}

		/// <summary>
		/// Returns the underlying stream with its Position synced to the reader's current position.
		/// </summary>
		Stream IDwgStreamReader.Stream
		{
			get
			{
				if (_hasBuf)
					this._stream.Position = _bufPos;
				return _stream;
			}
		}

		public override long Length => _hasBuf ? _bufEnd : this._stream.Length;

		public bool IsEmpty { get; private set; } = false;

		protected byte _lastByte;

		public DwgStreamReaderBase(Stream stream, bool resetPosition) : base(stream, resetPosition)
		{
			if (this.RawBuffer != null)
			{
				_buf = this.RawBuffer;
				_bufPos = this.RawBufferOffset + (int)this._stream.Position;
				_bufEnd = this.RawBufferOffset + (int)this._stream.Length;
				_hasBuf = true;
			}
			else if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> seg))
			{
				_buf = seg.Array;
				_bufPos = seg.Offset + (int)ms.Position;
				_bufEnd = seg.Offset + seg.Count;
				_hasBuf = true;
			}
		}

		public static IDwgStreamReader GetStreamHandler(ACadVersion version, Stream stream, Encoding encoding = null, bool resetPositon = false)
		{
			IDwgStreamReader reader = null;

			switch (version)
			{
				case ACadVersion.Unknown:
					throw new Exception();
				case ACadVersion.MC0_0:
				case ACadVersion.AC1_2:
				case ACadVersion.AC1_4:
				case ACadVersion.AC1_50:
				case ACadVersion.AC2_10:
				case ACadVersion.AC1002:
				case ACadVersion.AC1003:
				case ACadVersion.AC1004:
				case ACadVersion.AC1006:
				case ACadVersion.AC1009:
					throw new NotSupportedException($"Dwg version not supported: {version}");
				case ACadVersion.AC1012:
				case ACadVersion.AC1014:
					reader = new DwgStreamReaderAC12(stream, resetPositon);
					break;
				case ACadVersion.AC1015:
					reader = new DwgStreamReaderAC15(stream, resetPositon);
					break;
				case ACadVersion.AC1018:
					reader = new DwgStreamReaderAC18(stream, resetPositon);
					break;
				case ACadVersion.AC1021:
					reader = new DwgStreamReaderAC21(stream, resetPositon);
					break;
				case ACadVersion.AC1024:
				case ACadVersion.AC1027:
				case ACadVersion.AC1032:
					reader = new DwgStreamReaderAC24(stream, resetPositon);
					break;
				default:
					throw new NotSupportedException($"Dwg version not supported: {version}");
			}

			if (encoding != null)
			{
				reader.Encoding = encoding;
			}

			return reader;
		}

		public override byte ReadByte()
		{
			if (_hasBuf)
			{
				if (this.BitShift == 0)
				{
					_lastByte = _buf[_bufPos++];
					return _lastByte;
				}
				byte lastValues = (byte)((uint)_lastByte << BitShift);
				_lastByte = _buf[_bufPos++];
				return (byte)(lastValues | (uint)(byte)((uint)_lastByte >> 8 - BitShift));
			}

			if (this.BitShift == 0)
			{
				//No need to apply the shift
				_lastByte = base.ReadByte();

				return _lastByte;
			}

			//Get the last bits from the last readed byte
			byte lastValues2 = (byte)((uint)_lastByte << BitShift);

			_lastByte = base.ReadByte();

			return (byte)(lastValues2 | (uint)(byte)((uint)_lastByte >> 8 - BitShift));
		}

		public byte[] ReadBytes(int length)
        {
            byte[] numArray = new byte[length];
            this.applyShiftToArr(length, numArray);
            return numArray;
        }

		public override void ReadBytes(byte[] numArray, int length)
		{
			this.applyShiftToArr(length, numArray);
		}

		#region Inline numeric reads — bypass converter allocation on Mono

		// These 'new' methods hide StreamIO's generic Read*<T>() methods to eliminate
		// per-call 'new T()' converter allocation. DWG is always little-endian and
		// macOS/x86 are little-endian, so BitConverter gives the correct result directly.

		public new short ReadShort<T>() where T : IEndianConverter, new()
		{
			if (_hasBuf && BitShift == 0)
			{
				short val = BitConverter.ToInt16(_buf, _bufPos);
				_bufPos += 2;
				return val;
			}
			byte[] buffer = ByteArray2;
			applyShiftToArr(2, buffer);
			return BitConverter.ToInt16(buffer, 0);
		}

		public new ushort ReadUShort<T>() where T : IEndianConverter, new()
		{
			if (_hasBuf && BitShift == 0)
			{
				ushort val = BitConverter.ToUInt16(_buf, _bufPos);
				_bufPos += 2;
				return val;
			}
			byte[] buffer = ByteArray2;
			applyShiftToArr(2, buffer);
			return BitConverter.ToUInt16(buffer, 0);
		}

		public new int ReadInt<T>() where T : IEndianConverter, new()
		{
			if (_hasBuf && BitShift == 0)
			{
				int val = BitConverter.ToInt32(_buf, _bufPos);
				_bufPos += 4;
				return val;
			}
			byte[] buffer = ByteArray4;
			applyShiftToArr(4, buffer);
			return BitConverter.ToInt32(buffer, 0);
		}

		public new uint ReadUInt<T>() where T : IEndianConverter, new()
		{
			if (_hasBuf && BitShift == 0)
			{
				uint val = BitConverter.ToUInt32(_buf, _bufPos);
				_bufPos += 4;
				return val;
			}
			byte[] buffer = ByteArray4;
			applyShiftToArr(4, buffer);
			return BitConverter.ToUInt32(buffer, 0);
		}

		public new double ReadDouble<T>() where T : IEndianConverter, new()
		{
			if (_hasBuf && BitShift == 0)
			{
				double val = BitConverter.ToDouble(_buf, _bufPos);
				_bufPos += 8;
				return val;
			}
			byte[] buffer = ByteArray8;
			applyShiftToArr(8, buffer);
			return BitConverter.ToDouble(buffer, 0);
		}

		public new long ReadLong<T>() where T : IEndianConverter, new()
		{
			if (_hasBuf && BitShift == 0)
			{
				long val = BitConverter.ToInt64(_buf, _bufPos);
				_bufPos += 8;
				return val;
			}
			byte[] buffer = ByteArray8;
			applyShiftToArr(8, buffer);
			return BitConverter.ToInt64(buffer, 0);
		}

		public new ulong ReadULong<T>() where T : IEndianConverter, new()
		{
			if (_hasBuf && BitShift == 0)
			{
				ulong val = BitConverter.ToUInt64(_buf, _bufPos);
				_bufPos += 8;
				return val;
			}
			byte[] buffer = ByteArray8;
			applyShiftToArr(8, buffer);
			return BitConverter.ToUInt64(buffer, 0);
		}

		// Non-generic wrappers that route through our optimized generic methods
		// (StreamIO's non-generic versions would call StreamIO's generic versions, bypassing our optimization)
		public new short ReadShort() => this.ReadShort<DefaultEndianConverter>();
		public new ushort ReadUShort() => this.ReadUShort<DefaultEndianConverter>();
		public new int ReadInt() => this.ReadInt<DefaultEndianConverter>();
		public new uint ReadUInt() => this.ReadUInt<DefaultEndianConverter>();
		public new double ReadDouble() => this.ReadDouble<DefaultEndianConverter>();
		public new long ReadLong() => this.ReadLong<DefaultEndianConverter>();
		public new ulong ReadULong() => this.ReadULong<DefaultEndianConverter>();

		#endregion

		public long SetPositionByFlag(long position)
		{
			this.SetPositionInBits(position);

			//String stream present bit (last bit in pre-handles section).
			bool flag = this.ReadBit();

			long startPositon = position;
			if (flag)
			{
				//String stream present				this.IsEmpty = false;
				//If 1, then the “endbit” location should be decremented by 16 bytes
				this.applyFlagToPosition(position, out long length, out long size);

				startPositon = length - size;

				this.SetPositionInBits(startPositon);
			}
			else
			{
				//Mark as empty
				this.IsEmpty = true;
				//There is no information, set the position to the end
				this.Position = this.Stream.Length;
			}

			return startPositon;
		}

		#region Read BIT CODES AND DATA DEFINITIONS

		/// <inheritdoc/>
		public bool ReadBit()
		{
			if (this.BitShift == 0)
			{
				this.AdvanceByte();
				bool result = (this._lastByte & 128) == 128;
				this.BitShift = 1;
				return result;
			}

			bool value = (this._lastByte << this.BitShift & 128) == 128;

			++this.BitShift;
			this.BitShift &= 7;

			return value;
		}

		/// <inheritdoc/>
		public short ReadBitAsShort()
		{
			return this.ReadBit() ? (short)1 : (short)0;
		}

		/// <inheritdoc/>
		public byte Read2Bits()
		{
			byte value;
			if (this.BitShift == 0)
			{
				this.AdvanceByte();
				value = (byte)((uint)this._lastByte >> 6);
				this.BitShift = 2;
			}
			else if (this.BitShift == 7)
			{
				byte lastValue = (byte)(this._lastByte << 1 & 2);
				this.AdvanceByte();
				value = (byte)(lastValue | (uint)(byte)((uint)this._lastByte >> 7));
				this.BitShift = 1;
			}
			else
			{
				value = (byte)(this._lastByte >> 6 - this.BitShift & 3);
				++this.BitShift;
				++this.BitShift;
				this.BitShift &= 7;
			}

			return value;
		}

		/// <inheritdoc/>
		public short ReadBitShort()
		{
			short value;
			switch (this.Read2Bits())
			{
				case 0:
					//00 : A short (2 bytes) follows, little-endian order (LSB first)
					value = this.ReadShort<LittleEndianConverter>();
					break;
				case 1:
					//01 : An unsigned char (1 byte) follows
					if (this.BitShift == 0)
					{
						this.AdvanceByte();
						value = this._lastByte;
						break;
					}
					value = this.applyShiftToLasByte();
					break;
				case 2:
					//10 : 0
					value = 0;
					break;
				case 3:
					//11 : 256
					value = 256;
					break;
				default:
					throw new Exception();
			}
			return value;
		}

		/// <inheritdoc/>
		public bool ReadBitShortAsBool()
		{
			return this.ReadBitShort() != 0;
		}

		/// <inheritdoc/>
		public int ReadBitLong()
		{
			int value;
			switch (this.Read2Bits())
			{
				case 0:
					//00 : A long (4 bytes) follows, little-endian order (LSB first)
					value = this.ReadInt<LittleEndianConverter>();
					break;
				case 1:
					//01 : An unsigned char (1 byte) follows
					if (this.BitShift == 0)
					{
						this.AdvanceByte();
						value = this._lastByte;
						break;
					}
					value = this.applyShiftToLasByte();
					break;
				case 2:
					//10 : 0
					value = 0;
					break;
				default:
					//11 : not used
					throw new Exception();
			}
			return value;
		}

		/// <inheritdoc/>
		public long ReadBitLongLong()
		{
			ulong value = 0;
			byte size = this.read3bits();

			for (int i = 0; i < size; ++i)
			{
				ulong b = this.ReadByte();
				value += b << (i << 3);
			}

			return (long)value;
		}

		/// <inheritdoc/>
		public double ReadBitDouble()
		{
			double value;
			switch (this.Read2Bits())
			{
				case 0:
					value = this.ReadDouble<LittleEndianConverter>();
					break;
				case 1:
					value = 1.0;
					break;
				case 2:
					value = 0.0;
					break;
				default:
					throw new Exception();
			}

			return value;
		}

		/// <inheritdoc/>
		public XY Read2BitDouble()
		{
			return new XY(this.ReadBitDouble(), this.ReadBitDouble());
		}

		/// <inheritdoc/>
		public XYZ Read3BitDouble()
		{
			return new XYZ(this.ReadBitDouble(), this.ReadBitDouble(), this.ReadBitDouble());
		}

		/// <inheritdoc/>
		public char ReadRawChar()
		{
			return (char)this.ReadByte();
		}

		/// <inheritdoc/>
		public long ReadRawLong()
		{
			return this.ReadInt<LittleEndianConverter>();
		}

		/// <inheritdoc/>
		public ulong ReadRawULong()
		{
			return this.ReadULong<LittleEndianConverter>();
		}

		/// <inheritdoc/>
		public XY Read2RawDouble()
		{
			return new XY(this.ReadDouble(), this.ReadDouble());
		}

		/// <inheritdoc/>
		public ulong ReadModularChar()
		{
			int shift = 0;
			byte lastByte = this.ReadByte();

			//Remove the flag
			ulong value = (ulong)(lastByte & 0b01111111);

			if ((lastByte & 0b10000000) != 0)
			{
				while (true)
				{
					shift += 7;
					byte last = this.ReadByte();
					value |= (ulong)(last & 0b01111111) << shift;

					//Check flag
					if ((last & 0b10000000) == 0)
						break;
				}
			}

			return value;
		}

		/// <inheritdoc/>
		public int ReadSignedModularChar()
		{
			//Modular characters are a method of storing compressed integer values. They are used in the object map to
			//indicate both handle offsets and file location offsets.They consist of a stream of bytes, terminating when
			//the high bit of the byte is 0.
			int value;

			if (this.BitShift == 0)
			{
				//No shift, read normal
				this.AdvanceByte();

				//Check if the current byte
				if ((this._lastByte & 0b10000000) == 0) //Check the flag
				{
					//Drop the flags
					value = this._lastByte & 0b00111111;

					//Check the sign flag
					if ((this._lastByte & 0b01000000) > 0U)
						value = -value;
				}
				else
				{
					int totalShift = 0;
					int sum = this._lastByte & sbyte.MaxValue;
					while (true)
					{
						//Shift to apply
						totalShift += 7;
						this.AdvanceByte();

						//Check if the highest byte is 0
						if ((this._lastByte & 0b10000000) != 0)
							sum |= (this._lastByte & sbyte.MaxValue) << totalShift;
						else
							break;
					}

					//Drop the flags at the las byte, and add it's value
					value = sum | (this._lastByte & 0b00111111) << totalShift;

					//Check the sign flag
					if ((this._lastByte & 0b01000000) > 0U)
						value = -value;
				}
			}
			else
			{
				//Apply the shift to each byte
				byte lastByte = this.applyShiftToLasByte();
				if ((lastByte & 0b10000000) == 0)
				{
					//Drop the flags
					value = lastByte & 0b00111111;

					//Check the sign flag
					if ((lastByte & 0b01000000) > 0U)
						value = -value;
				}
				else
				{
					int totalShift = 0;
					int sum = lastByte & sbyte.MaxValue;
					byte currByte;
					while (true)
					{
						//Shift to apply
						totalShift += 7;
						currByte = this.applyShiftToLasByte();

						//Check if the highest byte is 0
						if ((currByte & 0b10000000) != 0)
							sum |= (currByte & sbyte.MaxValue) << totalShift;
						else
							break;
					}

					//Drop the flags at the las byte, and add it's value
					value = sum | (currByte & 0b00111111) << totalShift;

					//Check the sign flag
					if ((currByte & 0b01000000) > 0U)
						value = -value;
				}
			}
			return value;
		}

		/// <inheritdoc/>
		public int ReadModularShort()
		{
			int shift = 0b1111;

			//Read the bytes that form the short
			byte b1 = this.ReadByte();
			byte b2 = this.ReadByte();

			bool flag = (b2 & 0b10000000) == 0;

			//Set the value in little endian
			int value = b1 | (b2 & 0b1111111) << 8;

			while (!flag)
			{
				//Read 2 more bytes
				b1 = this.ReadByte();
				b2 = this.ReadByte();

				//Check the flag
				flag = (b2 & 0b10000000) == 0;

				//Set the value in little endian
				value |= b1 << shift;
				shift += 8;
				value |= (b2 & 0b1111111) << shift;

				//Update the shift
				shift += 7;
			}

			return value;
		}

		#region Handle reference

		/// <inheritdoc/>
		public ulong HandleReference()
		{
			if(this.HandleReference(0UL, out DwgReferenceType _, out ulong handle))
				return handle;
            throw new DwgException($"[HandleReference] invalid reference code ");
        }

		/// <inheritdoc/>
		public ulong HandleReference(ulong referenceHandle)
		{
			if (this.HandleReference(referenceHandle, out DwgReferenceType _, out ulong handle))
				return handle;
            throw new DwgException($"[HandleReference] invalid reference code ");
        }

		/// <inheritdoc/>
		public bool HandleReference(ulong referenceHandle, out DwgReferenceType reference, out ulong handle)
		{
			//|CODE (4 bits)|COUNTER (4 bits)|HANDLE or OFFSET|
			byte form = this.ReadByte();

			//CODE of the reference
			byte code = (byte)((uint)form >> 4);
			//COUNTER tells how many bytes of HANDLE follow.
			int counter = form & 0b00001111;

			//Get the reference type reading the last 2 bits
			reference = (DwgReferenceType)((uint)code & 0b0011);
			handle = default;

			//0x2, 0x3, 0x4, 0x5	none - just read offset and use it as the result
			if (code <= 0x5)
				handle = this.readHandle(counter);
			//0x6	result is reference handle + 1 (length is 0 in this case)
			else if (code == 0x6)
				handle = ++referenceHandle;
			//0x8	result is reference handle – 1 (length is 0 in this case)
			else if (code == 0x8)
				handle = --referenceHandle;
			//0xA	result is reference handle plus offset
			else if (code == 0xA)
			{
				ulong offset = this.readHandle(counter);
				handle = referenceHandle + offset;
			}
			//0xC	result is reference handle minus offset
			else if (code == 0xC)
			{
				ulong offset = this.readHandle(counter);
				handle = referenceHandle - offset;
			}
			else
			{
				return false;
				//throw new DwgException($"[HandleReference] invalid reference code with value: {code}");
			}

			return true;
		}

		public ulong readHandle(int length)
		{
			var raw = ArrayPool<byte>.Shared.Rent(length);
			var arr = ByteArray8;

			if (_hasBuf)
			{
				if (_bufPos + length > _bufEnd)
				{
					ArrayPool<byte>.Shared.Return(raw);
					throw new EndOfStreamException();
				}
				System.Buffer.BlockCopy(_buf, _bufPos, raw, 0, length);
				_bufPos += length;
			}
			else if (this.Stream.Read(raw, 0, length) < length)
			{
				ArrayPool<byte>.Shared.Return(raw);
				throw new EndOfStreamException();
			}

			if (this.BitShift == 0)
			{
				//Set the array backwards
				for (int i = 0; i < length; ++i)
					arr[length - 1 - i] = raw[i];
			}
			else
			{
				int shift = 8 - this.BitShift;
				for (int i = 0; i < length; ++i)
				{
					//Get the last byte value
					byte lastByteValue = (byte)((uint)this._lastByte << this.BitShift);
					//Save the last byte
					this._lastByte = raw[i];
					//Add the value of the next byte to the current
					byte value = (byte)(lastByteValue | (uint)(byte)((uint)this._lastByte >> shift));
					//Save the value into the array
					arr[length - 1 - i] = value;
				}
			}

			//Set the left bytes to 0
			for (int index = length; index < 8; ++index)
				arr[index] = 0;

            ArrayPool<byte>.Shared.Return(raw);
			return LittleEndianConverter.Instance.ToUInt64(arr);
        }

		#endregion Handle reference

		/// <inheritdoc/>
		public virtual string ReadTextUnicode()
		{
			int textLength = this.ReadShort();
			int encodingKey = this.ReadByte();
			string value;

			if (textLength == 0)
			{
				value = string.Empty;
			}
			else
			{
				value = this.ReadString(textLength, TextEncoding.GetListedEncoding((CodePage)encodingKey));
			}

			return value;
		}

		/// <inheritdoc/>
		public virtual string ReadVariableText()
		{
			short length = this.ReadBitShort();
			string str;
			if (length > 0)
			{
				str = this.ReadString(length, this.Encoding);
				str = str.Replace("\0", "");
			}
			else
				str = string.Empty;
			return str;
		}

		/// <inheritdoc/>
		public byte[] ReadSentinel()
		{
			this.ReadBytes(ByteArray16, 16);
			return ByteArray16;
		}

		/// <inheritdoc/>
		public XY Read2BitDoubleWithDefault(XY defValues)
		{
			return new XY(
				ReadBitDoubleWithDefault(defValues.X),
				ReadBitDoubleWithDefault(defValues.Y));
		}

		/// <inheritdoc/>
		public XYZ Read3BitDoubleWithDefault(XYZ defValues)
		{
			return new XYZ(
				this.ReadBitDoubleWithDefault(defValues.X),
				this.ReadBitDoubleWithDefault(defValues.Y),
				this.ReadBitDoubleWithDefault(defValues.Z));
		}

		/// <inheritdoc/>
		public virtual Color ReadCmColor(out string colorName, out string bookName)
		{
			//R15 and earlier: BS color index
			short colorIndex = this.ReadBitShort();

			colorName = string.Empty;
			bookName = string.Empty;

			return new Color(colorIndex);
		}

		/// <inheritdoc/>
		public virtual Color ReadEnColor(out Transparency transparency, out bool flag)
		{
			flag = false;

			//BS : color index (always 0)
			short colorNumber = this.ReadBitShort();
			transparency = Transparency.ByLayer;

			return new Color(colorNumber);
		}

		public Color ReadColorByIndex()
		{
			return new Color(this.ReadBitShort());
		}

		/// <inheritdoc/>
		public virtual ObjectType ReadObjectType()
		{
			//Until R2007, the object type was a bit short.
			return (ObjectType)this.ReadBitShort();
		}

		/// <inheritdoc/>
		public virtual XYZ ReadBitExtrusion()
		{
			//For R13-R14 this is 3BD.
			return this.Read3BitDouble();
		}

		/// <inheritdoc/>
		public double ReadBitDoubleWithDefault(double def)
		{
			//Get the bytes form the default value
			byte[] arr = LittleEndianConverter.Instance.GetBytes(def);

			switch (this.Read2Bits())
			{
				//00 No more data present, use the value of the default double.
				case 0:
					return def;
				//01 4 bytes of data are present. The result is the default double, with the 4 data bytes patched in
				//replacing the first 4 bytes of the default double(assuming little endian).
				case 1:
					if (this.BitShift == 0)
					{
						this.AdvanceByte();
						arr[0] = this._lastByte;
						this.AdvanceByte();
						arr[1] = this._lastByte;
						this.AdvanceByte();
						arr[2] = this._lastByte;
						this.AdvanceByte();
						arr[3] = this._lastByte;
					}
					else
					{
						int shift = 8 - this.BitShift;
						arr[0] = (byte)((uint)this._lastByte << this.BitShift);
						this.AdvanceByte();
						arr[0] |= (byte)((uint)this._lastByte >> shift);
						arr[1] = (byte)((uint)this._lastByte << this.BitShift);
						this.AdvanceByte();
						arr[1] |= (byte)((uint)this._lastByte >> shift);
						arr[2] = (byte)((uint)this._lastByte << this.BitShift);
						this.AdvanceByte();
						arr[2] |= (byte)((uint)this._lastByte >> shift);
						arr[3] = (byte)((uint)this._lastByte << this.BitShift);
						this.AdvanceByte();
						arr[3] |= (byte)((uint)this._lastByte >> shift);
					}
					return LittleEndianConverter.Instance.ToDouble(arr);
				//10 6 bytes of data are present. The result is the default double, with the first 2 data bytes patched in
				//replacing bytes 5 and 6 of the default double, and the last 4 data bytes patched in replacing the first 4
				//bytes of the default double(assuming little endian).
				case 2:
					if (this.BitShift == 0)
					{
						this.AdvanceByte();
						arr[4] = this._lastByte;
						this.AdvanceByte();
						arr[5] = this._lastByte;
						this.AdvanceByte();
						arr[0] = this._lastByte;
						this.AdvanceByte();
						arr[1] = this._lastByte;
						this.AdvanceByte();
						arr[2] = this._lastByte;
						this.AdvanceByte();
						arr[3] = this._lastByte;
					}
					else
					{
						arr[4] = (byte)((uint)this._lastByte << this.BitShift);
						this.AdvanceByte();
						arr[4] |= (byte)((uint)this._lastByte >> 8 - this.BitShift);
						arr[5] = (byte)((uint)this._lastByte << this.BitShift);
						this.AdvanceByte();
						arr[5] |= (byte)((uint)this._lastByte >> 8 - this.BitShift);
						arr[0] = (byte)((uint)this._lastByte << this.BitShift);
						this.AdvanceByte();
						arr[0] |= (byte)((uint)this._lastByte >> 8 - this.BitShift);
						arr[1] = (byte)((uint)this._lastByte << this.BitShift);
						this.AdvanceByte();
						arr[1] |= (byte)((uint)this._lastByte >> 8 - this.BitShift);
						arr[2] = (byte)((uint)this._lastByte << this.BitShift);
						this.AdvanceByte();
						arr[2] |= (byte)((uint)this._lastByte >> 8 - this.BitShift);
						arr[3] = (byte)((uint)this._lastByte << this.BitShift);
						this.AdvanceByte();
						arr[3] |= (byte)((uint)this._lastByte >> 8 - this.BitShift);
					}
					return LittleEndianConverter.Instance.ToDouble(arr);
				//11 A full RD follows.
				case 3:
					return this.ReadDouble();
				default:
					throw new Exception();
			}
		}

		/// <inheritdoc/>
		public virtual double ReadBitThickness()
		{
			//For R13-R14, this is a BD.
			return this.ReadBitDouble();
		}

		#endregion Read BIT CODES AND DATA DEFINITIONS

		/// <inheritdoc/>
		public DateTime Read8BitJulianDate()
		{
			return this.julianToDate(this.ReadInt(), this.ReadInt());
		}

		/// <inheritdoc/>
		public DateTime ReadDateTime()
		{
			return this.julianToDate(this.ReadBitLong(), this.ReadBitLong());
		}

		/// <inheritdoc/>
		public TimeSpan ReadTimeSpan()
		{
			long hours = this.ReadBitLong();
			long milliseconds = this.ReadBitLong();

			// Handle potential overflow
			if (hours < 0 || hours > TimeSpan.MaxValue.TotalHours || milliseconds < 0 || milliseconds > TimeSpan.MaxValue.TotalMilliseconds)
			{
				return TimeSpan.FromHours(0) + TimeSpan.FromMilliseconds(0);

			}

			return TimeSpan.FromHours(hours) + TimeSpan.FromMilliseconds(milliseconds);
		}

		#region Stream pointer control

		/// <inheritdoc/>
		public long PositionInBits()
		{
			long bitPosition = this.Position * 8L;

			if ((uint)this.BitShift > 0U)
				bitPosition += this.BitShift - 8;

			return bitPosition;
		}

		/// <inheritdoc/>
		public void SetPositionInBits(long position)
		{
			this.Position = position >> 3;
			this.BitShift = (int)(position & 7L);

			if ((uint)this.BitShift <= 0U)
				return;

			this.AdvanceByte();
		}

		/// <inheritdoc/>
		public void AdvanceByte()
		{
			if (_hasBuf)
				this._lastByte = _buf[_bufPos++];
			else
				this._lastByte = base.ReadByte();
		}

		/// <inheritdoc/>
		public void Advance(int offset)
		{
			if (offset > 1)
			{
				if (_hasBuf)
					_bufPos += offset - 1;
				else
					this.Stream.Position += offset - 1;
			}

			this.ReadByte();
		}

		/// <inheritdoc/>
		public ushort ResetShift()
		{
			//Reset the shift value
			if ((uint)this.BitShift > 0U)
				this.BitShift = 0;

			this.AdvanceByte();
			ushort num = this._lastByte;
			this.AdvanceByte();

			return (ushort)(num | (uint)(ushort)((uint)this._lastByte << 8));
		}

		#endregion Stream pointer control

		protected void applyFlagToPosition(long lastPos, out long length, out long strDataSize)
		{
			//If 1, then the “endbit” location should be decremented by 16 bytes

			length = lastPos - 16L;
			this.SetPositionInBits(length);

			//short should be read at location endbit – 128 (bits)
			strDataSize = this.ReadUShort();

			//If this short has the 0x8000 bit set,
			//then decrement endbit by an additional 16 bytes,
			//strip the 0x8000 bit off of strDataSize, and read
			//the short at this new location, calling it hiSize.
			if (((ulong)strDataSize & 0x8000) <= 0UL)
				return;

			length -= 16;

			this.SetPositionInBits(length);

			strDataSize &= short.MaxValue;

			int hiSize = this.ReadUShort();
			//Then set strDataSize to (strDataSize | (hiSize << 15))
			strDataSize += (hiSize & ushort.MaxValue) << 15;

			//All unicode strings in this object are located in the “string stream”,
			//and should be read from this stream, even though the location of the
			//TV type fields in the object descriptions list these fields in among
			//the normal object data.
		}

		protected byte applyShiftToLasByte()
		{
			byte value = (byte)((uint)this._lastByte << this.BitShift);

			this.AdvanceByte();

			return (byte)((uint)value | (byte)((uint)this._lastByte >> 8 - this.BitShift));
		}

		private void applyShiftToArr(int length, byte[] arr)
		{
			if (_hasBuf)
			{
				//Read directly from buffer
				if (_bufPos + length > _bufEnd)
					throw new EndOfStreamException();
				System.Buffer.BlockCopy(_buf, _bufPos, arr, 0, length);
				_bufPos += length;
			}
			else
			{
				//Read from stream
				if (this.Stream.Read(arr, 0, length) != length)
					throw new EndOfStreamException();
			}

			if ((uint)this.BitShift <= 0U)
				return;

			int shift = 8 - this.BitShift;
			for (int i = 0; i < length; ++i)
			{
				//Get the last byte value
				byte lastByteValue = (byte)((uint)this._lastByte << this.BitShift);
				//Save the last byte
				this._lastByte = arr[i];
				//Add the value of the next byte to the current
				byte value = (byte)(lastByteValue | (uint)(byte)((uint)this._lastByte >> shift));
				//Save the value into the array
				arr[i] = value;
			}
		}

		private byte read3bits()
		{
			byte b1 = 0;
			if (this.ReadBit())
				b1 = 1;
			byte b2 = (byte)((uint)b1 << 1);
			if (this.ReadBit())
				b2 |= 1;
			byte b3 = (byte)((uint)b2 << 1);
			if (this.ReadBit())
				b3 |= 1;
			return b3;
		}

		private DateTime julianToDate(int jdate, int miliseconds)
		{
			double unixTime = (jdate - 2440587.5) * 86400;

			DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);

			try
			{
				dtDateTime = dtDateTime.AddSeconds(unixTime).ToLocalTime();
			}
			catch (Exception)
			{
				dtDateTime = DateTime.MinValue;
			}

			return dtDateTime.AddMilliseconds(miliseconds);
		}
	}
}