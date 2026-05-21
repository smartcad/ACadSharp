using ACadSharp.Entities;
using ACadSharp.Objects;

namespace ACadSharp.IO.Templates
{
    internal sealed class DimAssocTemplate : Templates.CadTemplate<DimensionAssociativity>
    {
        public ulong? DimensionHandle { get; set; }
        public ulong[] MainGeometryHandle { get; set; }

        public DimAssocTemplate(DimensionAssociativity cadObject) : base(cadObject)
        {
        }

        public override void Build(CadDocumentBuilder builder)
        {
            base.Build(builder);

            if (builder.TryGetCadObject(this.DimensionHandle, out Dimension dim))
            {
                this.CadObject.DimensionObject = dim;
            }
            
            if (this.MainGeometryHandle == null)
                return;

            for (var i = 0; i < this.MainGeometryHandle.Length; i++)
            {
                if (builder.TryGetCadObject(this.MainGeometryHandle[i], out Entity record))
                {
                    this.CadObject.PointRefs[i].Geometry = record;
                }
            }
        }
    }
}
