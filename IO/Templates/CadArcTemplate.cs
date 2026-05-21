using System;
using ACadSharp.Entities;

namespace ACadSharp.IO.Templates
{
	[Obsolete]
	internal class CadArcTemplate : CadEntityTemplate
	{

		public CadArcTemplate(Entity entity) : base(entity) { }


        public override void Build(CadDocumentBuilder builder)
		{
			base.Build(builder);

			return;
		}
	}
}
