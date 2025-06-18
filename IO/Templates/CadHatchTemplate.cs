using ACadSharp.Entities;
using System.Collections.Generic;

namespace ACadSharp.IO.Templates
{
	internal partial class CadHatchTemplate : CadEntityTemplate<Hatch>
	{

		public List<CadBoundaryPathTemplate> PathTempaltes = new List<CadBoundaryPathTemplate>();

		public CadHatchTemplate() : base(new Hatch()) { }

		public CadHatchTemplate(Hatch hatch) : base(hatch) { }

		public override void Build(CadDocumentBuilder builder)
		{
			base.Build(builder);

			Hatch hatch = CadObject as Hatch;

			foreach (CadBoundaryPathTemplate t in PathTempaltes)
			{
				(this.CadObject as Hatch).Paths.Add(t.Path);
				t.Build(builder);
			}
		}
	}
}
