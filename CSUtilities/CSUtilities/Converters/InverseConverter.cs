using System;

namespace CSUtilities.Converters
{
	internal class InverseConverter : IEndianConverter
	{
		public byte[] GetBytes(char value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			InverseConverter.fullInverse(bytes);
			return bytes;
		}
		public byte[] GetBytes(short value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			InverseConverter.fullInverse(bytes);
			return bytes;
		}
		public byte[] GetBytes(ushort value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			InverseConverter.fullInverse(bytes);
			return bytes;
		}
		public byte[] GetBytes(int value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			InverseConverter.fullInverse(bytes);
			return bytes;
		}
		public byte[] GetBytes(uint value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			InverseConverter.fullInverse(bytes);
			return bytes;
		}
		public byte[] GetBytes(long value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			InverseConverter.fullInverse(bytes);
			return bytes;
		}
		public byte[] GetBytes(ulong value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			InverseConverter.fullInverse(bytes);
			return bytes;
		}
		public byte[] GetBytes(double value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			InverseConverter.fullInverse(bytes);
			return bytes;
		}
		public byte[] GetBytes(float value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			InverseConverter.fullInverse(bytes);
			return bytes;
		}
		public char ToChar(byte[] arr) 
		{
			InverseConverter.fullInverse(arr);
			return BitConverter.ToChar(arr, 0);
		}
		public short ToInt16(byte[] arr) 
		{
			InverseConverter.fullInverse(arr);
			return BitConverter.ToInt16(arr, 0);
		}
		public ushort ToUInt16(byte[] arr) 
		{
			InverseConverter.fullInverse(arr);
			return BitConverter.ToUInt16(arr, 0);
		}
		public int ToInt32(byte[] arr) 
		{
			InverseConverter.fullInverse(arr);
			return BitConverter.ToInt32(arr, 0);
		}
		public uint ToUInt32(byte[] arr) 
		{
			InverseConverter.fullInverse(arr);
			return BitConverter.ToUInt32(arr, 0);
		}
		public long ToInt64(byte[] arr) 
		{
			InverseConverter.fullInverse(arr);
			return BitConverter.ToInt64(arr, 0);
		}
		public ulong ToUInt64(byte[] arr) 
		{
			InverseConverter.fullInverse(arr);
			return BitConverter.ToUInt64(arr, 0);
		}
		public double ToDouble(byte[] arr) 
		{
			InverseConverter.fullInverse(arr);
			return BitConverter.ToDouble(arr, 0);
		}
		public float ToSingle(byte[] arr)
		{
			InverseConverter.fullInverse(arr);
			return BitConverter.ToSingle(arr, 0);
		}

		public char ToChar(byte[] arr, int offset)
		{
			InverseConverter.fullInverse(arr);
            return BitConverter.ToChar(arr, 0);
		}
		public short ToInt16(byte[] arr, int offset) => BitConverter.ToInt16(InverseConverter.fullInverse(arr, offset), 0);
		public ushort ToUInt16(byte[] arr, int offset) => BitConverter.ToUInt16(InverseConverter.fullInverse(arr, offset), 0);
		public int ToInt32(byte[] arr, int offset) => BitConverter.ToInt32(InverseConverter.fullInverse(arr, offset), 0);
		public uint ToUInt32(byte[] arr, int offset) => BitConverter.ToUInt32(InverseConverter.fullInverse(arr, offset), 0);
		public long ToInt64(byte[] arr, int offset) => BitConverter.ToInt64(InverseConverter.fullInverse(arr, offset), 0);
		public ulong ToUInt64(byte[] arr, int offset) => BitConverter.ToUInt64(InverseConverter.fullInverse(arr, offset), 0);
		public double ToDouble(byte[] arr, int offset) => BitConverter.ToDouble(InverseConverter.fullInverse(arr, offset), 0);
		public float ToSingle(byte[] arr, int offset) => BitConverter.ToSingle(InverseConverter.fullInverse(arr, offset), 0);

		public ReadOnlySpan<byte> GetBytes<T>(T value)
			where T : struct
		{
			switch (value)
			{
				case char c:
					return this.GetBytes(c);
				case short s:
					return this.GetBytes(s);
				case ushort us:
					return this.GetBytes(us);
				case int i:
					return this.GetBytes(i);
				case uint ui:
					return this.GetBytes(ui);
				case long l:
					return this.GetBytes(l);
				case ulong ul:
					return this.GetBytes(ul);
				case double d:
					return this.GetBytes(d);
				case float f:
					return this.GetBytes(f);
				default:
					throw new NotSupportedException($"type {typeof(T).FullName} not supported");
			}
		}

		private static void fullInverse(in byte[] arr)
		{
			Span<byte> inverse = stackalloc byte[arr.Length];
			arr.CopyTo(inverse);

			for (int i = arr.Length - 1, j = 0; j < arr.Length; i--, j++)
			{
				arr[i] = inverse[j];
			}
		}
		
		private static byte[] fullInverse(byte[] arr, int offset)
		{
			byte[] inverse = new byte[arr.Length];

			for (int i = arr.Length - 1, j = 0; j < arr.Length; i--, j++)
			{
				inverse[i] = arr[offset - j];
			}

			return inverse;
		}
	}
}
