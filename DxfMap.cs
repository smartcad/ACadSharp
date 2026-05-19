using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ACadSharp
{
	public class DxfMap : DxfMapBase
	{
		/// <summary>
		/// Cache of created DXF mapped classes.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, DxfMap> _cache = new ConcurrentDictionary<Type, DxfMap>();

		public Dictionary<string, DxfClassMap> SubClasses { get; internal set; } = new Dictionary<string, DxfClassMap>();

		/// <summary>
		/// Creates a dxf map for an <see cref="Entity"/> or a <see cref="TableEntry"/>
		/// </summary>
		/// <remarks>
		/// This method does not work with the entities <see cref="AttributeEntity"/> and <see cref="AttributeDefinition"/>
		/// </remarks>
		public static DxfMap Create<T>()
			where T : CadObject
		{
			return DxfMap.Create(typeof(T));
		}

		//TODO: change to public? Using the type parameter does not constraing the use of the method
		internal static DxfMap Create(Type type)
		{
			if (tryGetFromCache(type, out var map))
			{
				return map;
			}

			// Try generated registry first (no reflection)
			if (DxfMetadataRegistry.TryCreateMap(type, out map))
			{
				_cache.TryAdd(type, map);
				if (tryGetFromCache(type, out map))
				{
					return map;
				}
				return map;
			}

			throw new NotSupportedException(
				$"No DXF map is available for type '{type.FullName}'. " +
				"Ensure the type has the [DxfName] attribute and the source generator has run.");
		}

		/// <summary>
		/// Clears the map cache.
		/// </summary>
		public static void ClearCache()
		{
			_cache.Clear();
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return $"DxfMap:{this.Name}";
		}

		internal DxfMap Clone()
		{
			DxfMap map = new DxfMap();
			map.Name = this.Name;

			foreach (var p in this.DxfProperties)
			{
				map.DxfProperties.Add(p.Key, p.Value);
			}

			foreach (var sub in this.SubClasses)
			{
				map.SubClasses.Add(sub.Key, sub.Value);
			}

			return map;
		}

		private static bool tryGetFromCache(Type type, out DxfMap map)
		{
			return _cache.TryGetValue(type, out map);
		}
	}
}
