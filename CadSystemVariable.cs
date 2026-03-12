using ACadSharp.Attributes;
using ACadSharp.Header;
using System;
using System.Reflection;

namespace ACadSharp
{
	public class CadSystemVariable : DxfPropertyBase<CadSystemVariableAttribute>
	{
		public string Name { get { return this._attributeData.Name; } }

		public CadSystemVariable(PropertyInfo property) : base(property)
		{
		}

		internal CadSystemVariable(CadSystemVariableAttribute attributeData, Type propertyType, string propertyName,
			Func<object, object> getter, Action<object, object> setter)
			: base(attributeData, propertyType, propertyName, getter, setter)
		{
		}

		public object GetValue<THeader>(THeader obj)
			where THeader : CadHeader
		{
			return this._getter(obj);
		}

		internal void SetValue(CadHeader header, object value)
		{
			this._setter?.Invoke(header, value);
		}
	}
}
