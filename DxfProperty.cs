using ACadSharp.Attributes;
using System;
using System.Linq;
using System.Reflection;

namespace ACadSharp
{
	public class DxfProperty : DxfPropertyBase<DxfCodeValueAttribute>
	{
		private DxfCode[] _collectionCodes;

		/// <summary>
		/// Creates a dxf property referenced to an object property
		/// </summary>
		/// <remarks>
		/// The property must have the <see cref="DxfCodeValueAttribute"/>
		/// </remarks>
		/// <param name="property"></param>
		/// <exception cref="ArgumentException"></exception>
		public DxfProperty(PropertyInfo property) : base(property) { }

		/// <summary>
		/// Creates a dxf property referenced to an object property
		/// </summary>
		/// <remarks>
		/// The property must have the <see cref="DxfCodeValueAttribute"/>
		/// </remarks>
		/// <param name="code">assigned value for this property, only useful if the property has multiple codes assigned</param>
		/// <param name="property"></param>
		/// <exception cref="ArgumentException"></exception>
		public DxfProperty(int code, PropertyInfo property) : this(property)
		{
			if (!this._attributeData.ValueCodes.Contains((DxfCode)code))
				throw new ArgumentException($"The {nameof(DxfCodeValueAttribute)} does not have match with the code {code}", nameof(property));

			this._assignedCode = code;
		}

		internal DxfProperty(int assignedCode, DxfCodeValueAttribute attributeData, Type propertyType, string propertyName,
			Func<object, object> getter, Action<object, object> setter, DxfCode[] collectionCodes)
			: base(attributeData, propertyType, propertyName, getter, setter)
		{
			this._assignedCode = assignedCode;
			this._collectionCodes = collectionCodes;
		}

		public object GetValue<TCadObject>(TCadObject obj)
			where TCadObject : CadObject
		{
			return this._getter(obj);
		}

		public DxfCode[] GetCollectionCodes()
		{
			if (this._collectionCodes != null)
				return this._collectionCodes;

			return this._property?.GetCustomAttribute<DxfCollectionCodeValueAttribute>()?.ValueCodes;
		}

		public override string ToString()
		{
			string str = string.Empty;

			foreach (int code in this.DxfCodes)
			{
				str += $"{code}:";
			}

			str += this._propertyName;

			return str;
		}
	}
}
