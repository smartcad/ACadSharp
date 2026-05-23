using System;

namespace ACadSharp.Attributes
{
	[System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	sealed class DxfCollectionCodeValueAttribute : Attribute, ICodeValueAttribute
	{
		/// <inheritdoc/>
		public DxfCode[] ValueCodes { get; }

		/// <inheritdoc/>
		public DxfReferenceType ReferenceType { get; }

		public DxfCollectionCodeValueAttribute(params int[] codes)
		{
			var arr = new DxfCode[codes.Length];
			for (int i = 0; i < codes.Length; i++)
				arr[i] = (DxfCode)codes[i];
			this.ValueCodes = arr;
		}

		public DxfCollectionCodeValueAttribute(DxfReferenceType referenceType, params int[] codes) : this(codes)
		{
			this.ReferenceType = referenceType;
		}
	}
}
