using System;
using System.Collections.Generic;
using System.Text;

namespace ACadSharp.Attributes
{
	[System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class CadSystemVariableAttribute : Attribute, ICodeValueAttribute
	{
		/// <summary>
		/// System variable name
		/// </summary>
		public string Name { get; }

		/// <inheritdoc/>
		public DxfCode[] ValueCodes { get; }

		/// <inheritdoc/>
		public DxfReferenceType ReferenceType { get; }

		/// <summary>
		/// 
		/// </summary>
		public bool IsName { get; } = false;

		public CadSystemVariableAttribute(string variable, bool isName, params int[] codes)
		{
			this.Name = variable;
			this.IsName = isName;
			var arr = new DxfCode[codes.Length];
			for (int i = 0; i < codes.Length; i++)
				arr[i] = (DxfCode)codes[i];
			this.ValueCodes = arr;
		}

		public CadSystemVariableAttribute(string variable, params int[] codes)
		{
			this.Name = variable;
			var arr = new DxfCode[codes.Length];
			for (int i = 0; i < codes.Length; i++)
				arr[i] = (DxfCode)codes[i];
			this.ValueCodes = arr;
		}

		public CadSystemVariableAttribute(string variable, params DxfCode[] codes)
		{
			this.Name = variable;
			this.ValueCodes = codes;
		}

		public CadSystemVariableAttribute(DxfReferenceType referenceType, string variable, params int[] codes)
		{
			this.ReferenceType = referenceType;
			this.Name = variable;
			var arr = new DxfCode[codes.Length];
			for (int i = 0; i < codes.Length; i++)
				arr[i] = (DxfCode)codes[i];
			this.ValueCodes = arr;
		}
	}
}
