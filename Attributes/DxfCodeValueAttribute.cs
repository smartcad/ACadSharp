using System;

namespace ACadSharp.Attributes
{
	[System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class DxfCodeValueAttribute : Attribute, ICodeValueAttribute
	{
		/// <inheritdoc/>
		public DxfCode[] ValueCodes { get; }

		/// <inheritdoc/>
		public DxfReferenceType ReferenceType { get; }

		public DxfCodeValueAttribute(params int[] codes)
		{
			var arr = new DxfCode[codes.Length];
			for (int i = 0; i < codes.Length; i++)
				arr[i] = (DxfCode)codes[i];
			this.ValueCodes = arr;
		}

		public DxfCodeValueAttribute(DxfReferenceType referenceType, params int[] codes) : this(codes)
		{
			this.ReferenceType = referenceType;
		}
	}
}
