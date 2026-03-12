using System;
using System.Collections.Concurrent;

namespace ACadSharp
{
	public class DxfClassMap : DxfMapBase
	{
		/// <summary>
		/// Cache of created DXF mapped classes.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, DxfClassMap> _cache = new ConcurrentDictionary<Type, DxfClassMap>();

		public DxfClassMap() : base() { }

		public DxfClassMap(string name) : base()
		{
			this.Name = name;
		}

		/// <summary>
		/// Creates a DXF map of the passed type.
		/// </summary>
		/// <remarks>
		///   Will return a cached instance if it exists.  if not, it will be created on call.
		/// Use the <see cref="ClearCache"/> method to clear the cache and force a new mapping to be created.
		/// </remarks>
		/// <typeparam name="T">Type of CadObject to map.</typeparam>
		/// <returns>Mapped class</returns>
		public static DxfClassMap Create<T>()
			where T : CadObject
		{
			Type type = typeof(T);

			if (_cache.TryGetValue(type, out var classMap))
			{
				return classMap;
			}

			if (!DxfMetadataRegistry.TryGetClassMap(type, out classMap))
				throw new ArgumentException(
					$"{type.FullName} does not have a generated DXF class map. " +
					"Ensure the type has the [DxfSubClass] attribute and the source generator has run.");

			_cache.TryAdd(type, classMap);
			return classMap;
		}


		/// <summary>
		/// Clears the map cache.
		/// </summary>
		public void ClearCache()
		{
			_cache.Clear();
		}

		public override string ToString()
		{
			return $"DxfClassMap:{this.Name}";
		}
	}
}
