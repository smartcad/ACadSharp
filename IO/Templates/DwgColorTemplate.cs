using System;

namespace ACadSharp.IO.Templates
{
	internal class DwgColorTemplate : CadTemplate
	{
		public DwgColorTemplate(DwgColor color) : base(color) { }

		public override void Build(CadDocumentBuilder builder)
		{
			base.Build(builder);
		}

		public class DwgColor : CadObject
		{
			public override ObjectType ObjectType => ObjectType.INVALID;
			public Color Color { get; set; }
			public string BookName { get; set; }
			public string ColorName { get;set; }
			public override string SubclassMarker { get; }
		}
	}
}
