using System;
using System.Collections.Generic;

namespace ACadSharp
{
	public abstract class DxfMapBase
	{
		/// <summary>
		/// Name of the subclass map
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Properties linked to a dxf code
		/// </summary>
		public Dictionary<int, DxfProperty> DxfProperties { get; } = new Dictionary<int, DxfProperty>();

		protected static void addClassProperties(DxfMapBase map, Type type)
		{
			throw new NotSupportedException(
				"Reflection-based DXF map building is not supported. Use the generated DxfMetadataRegistry instead.");
		}

		protected static IEnumerable<KeyValuePair<int, DxfProperty>> cadObjectMapDxf(Type type)
		{
			throw new NotSupportedException(
				"Reflection-based DXF map building is not supported. Use the generated DxfMetadataRegistry instead.");
		}
	}
}
