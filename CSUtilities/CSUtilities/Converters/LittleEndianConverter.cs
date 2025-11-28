using System;
using System.Collections.Generic;
using System.Text;

namespace CSUtilities.Converters
{
	internal class LittleEndianConverter : EndianConverter
	{
		public static LittleEndianConverter Instance = new LittleEndianConverter();

		static IEndianConverter init()
		{
			if (BitConverter.IsLittleEndian)
				return (IEndianConverter)DefaultEndianConverter.Instance;
			else
				return (IEndianConverter)new InverseConverter();
		}

		private LittleEndianConverter() : base(init()) { }
	}
}
