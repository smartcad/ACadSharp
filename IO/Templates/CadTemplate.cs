using ACadSharp.Entities;
using ACadSharp.Objects;
using ACadSharp.Tables;
using System.Collections.Generic;

namespace ACadSharp.IO.Templates
{
	internal abstract class CadTemplate : ICadObjectTemplate
	{
		public CadObject CadObject { get; protected set; }

		public ulong? XDictHandle { get; set; }

		private List<ulong> _reactorsHandles;
		//public List<ulong> ReactorsHandles
		//{
		//	get => _reactorsHandles ??= new List<ulong>();
		//	set => _reactorsHandles = value;
		//}

		private Dictionary<ulong, ExtendedData> _eDataTemplate;

		private Dictionary<string, ExtendedData> _eDataTemplateByAppName;
		public Dictionary<string, ExtendedData> EDataTemplateByAppName
		{
			get => _eDataTemplateByAppName ??= new Dictionary<string, ExtendedData>();
			set => _eDataTemplateByAppName = value;
		}

		public CadTemplate(CadObject cadObject)
		{
			this.CadObject = cadObject;
		}

		public virtual void Build(CadDocumentBuilder builder)
		{
			if (builder.TryGetCadObject(this.XDictHandle, out CadDictionary cadDictionary))
			{
				this.CadObject.XDictionary = cadDictionary;
			}

			if (_reactorsHandles != null)
			{
				foreach (ulong handle in _reactorsHandles)
				{
					if (builder.TryGetCadObject(handle, out CadObject reactor))
					{
						if (this.CadObject.Reactors.ContainsKey(handle))
						{
							builder.Notify($"Reactor with handle {handle} already exist in the object {this.CadObject.Handle}", NotificationType.Warning);
						}
						else
						{
							this.CadObject.Reactors.Add(handle, reactor);
						}
					}
					else
					{
						builder.Notify($"Reactor with handle {handle} not found", NotificationType.Warning);
					}
				}
			}

			if(_eDataTemplate != null)
			{
				foreach(KeyValuePair<ulong, ExtendedData> item in _eDataTemplate)
				{
					if(builder.TryGetCadObject(item.Key, out AppId app))
					{
						this.CadObject.ExtendedData.Add(app, item.Value);
					}
				}
			}
		}

		protected IEnumerable<T> getEntitiesCollection<T>(CadDocumentBuilder builder, ulong firstHandle, ulong endHandle)
			where T : Entity
		{
			List<T> collection = new List<T>();

			if(!builder.TryGetCadObject(firstHandle, out Entity entity))
				return collection;

			while (entity != null)
			{
				collection.Add((T)entity);

				if (entity.Handle == endHandle)
				{
					break;
				}

				if (!entity.NextEntity.HasValue || !builder.TryGetCadObject(entity.NextEntity.Value, out entity))
				{
					if(!builder.TryGetCadObject(entity.Handle + 1, out entity))
						break;
				}
			}

			return collection;
		}

		protected bool getTableReference<T>(CadDocumentBuilder builder, ulong? handle, string name, out T reference)
			where T : TableEntry
		{
			if (builder.TryGetCadObject<T>(handle, out reference) || builder.TryGetTableEntry<T>(name, out reference))
			{
				return true;
			}
			else
			{
				if (!string.IsNullOrEmpty(name) || (handle.HasValue && handle.Value != 0))
				{
					builder.Notify($"{typeof(T).FullName} table reference with handle: {handle} | name: {name} not found for {this.CadObject.GetType().FullName} with handle {this.CadObject.Handle}", NotificationType.Warning);
				}

				return false;
			}
		}
	}
}
