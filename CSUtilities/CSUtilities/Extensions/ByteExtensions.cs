using System;
using System.Collections.Generic;
using System.Text;

namespace CSUtilities.Extensions
{
	internal static class ByteExtensions
	{
		/// <summary>
		/// Convert a byte array into a hex string array.
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		public static string ToHexString(this IEnumerable<byte> array)
		{
			int count = 0;
			if (array is ICollection<byte> col)
				count = col.Count;
			else if (array is byte[] arr)
				count = arr.Length;

			StringBuilder hex = new StringBuilder(count * 2);
			foreach (byte b in array)
				hex.AppendFormat("{0:X2}", b);
			return hex.ToString();
		}
	}
}
