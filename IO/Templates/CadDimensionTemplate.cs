using ACadSharp.Entities;
using ACadSharp.Tables;

namespace ACadSharp.IO.Templates
{
	internal class CadDimensionTemplate : CadEntityTemplate
	{
		public ulong? StyleHandle { get; set; }

		public ulong? BlockHandle { get; set; }

		public string BlockName { get; set; }

		public string StyleName { get; set; }


        private static readonly DimensionPlaceholder PlaceHolder = new DimensionPlaceholder();
		internal static readonly DimensionAligned Aligned = new DimensionAligned();

		public CadDimensionTemplate() : base(PlaceHolder) 
        {
            ClearDimensionProperties(PlaceHolder);
        }
		public CadDimensionTemplate(Dimension dimension) : base(dimension) { }

		public override void Build(CadDocumentBuilder builder)
		{
			if( this.CadObject == PlaceHolder || this.CadObject == Aligned)
				return;

			base.Build(builder);

			Dimension dimension = this.CadObject as Dimension;

			if (this.getTableReference(builder, this.StyleHandle, this.StyleName, out DimensionStyle style))
			{
				dimension.Style = style;
			}

			if (this.getTableReference(builder, this.BlockHandle, this.BlockName, out BlockRecord block))
			{
				dimension.Block = block;
			}
		}

        internal void SetDimensionObject(Dimension new_dim)
        {
            CopyDimensionProperties(PlaceHolder, new_dim);
            this.CadObject = new_dim;
        }

        public void SetDimensionFlags(DimensionType flags)
		{
			Dimension dimension = this.CadObject as Dimension;

			if (dimension is DimensionOrdinate ordinate)
			{
				ordinate.IsOrdinateTypeX = flags.HasFlag(DimensionType.OrdinateTypeX);
			}
			dimension.IsTextUserDefinedLocation = flags.HasFlag(DimensionType.TextUserDefinedLocation);
		}


        private static void CopyDimensionProperties(Dimension from_dimension, Dimension to_dimension)
        {
            to_dimension.Handle = from_dimension.Handle;
            to_dimension.Owner = from_dimension.Owner;

            //to_dimension.XDictionary = from_dimension.XDictionary;

            to_dimension.Color = from_dimension.Color;
            to_dimension.LineWeight = from_dimension.LineWeight;
            to_dimension.LinetypeScale = from_dimension.LinetypeScale;
            to_dimension.IsInvisible = from_dimension.IsInvisible;
            to_dimension.Transparency = from_dimension.Transparency;

            to_dimension.Version = from_dimension.Version;
            to_dimension.DefinitionPoint = from_dimension.DefinitionPoint;
            to_dimension.TextMiddlePoint = from_dimension.TextMiddlePoint;
            to_dimension.InsertionPoint = from_dimension.InsertionPoint;
            to_dimension.Normal = from_dimension.Normal;
            to_dimension.IsTextUserDefinedLocation = from_dimension.IsTextUserDefinedLocation;
            to_dimension.AttachmentPoint = from_dimension.AttachmentPoint;
            to_dimension.LineSpacingStyle = from_dimension.LineSpacingStyle;
            to_dimension.LineSpacingFactor = from_dimension.LineSpacingFactor;
            to_dimension.Text = from_dimension.Text;
            to_dimension.TextRotation = from_dimension.TextRotation;
            to_dimension.HorizontalDirection = from_dimension.HorizontalDirection;

            if(from_dimension is DimensionAligned dimensionAligned && to_dimension is DimensionLinear dimensionLinear)
            {
                dimensionLinear.FirstPoint = dimensionAligned.FirstPoint ;
                dimensionLinear.SecondPoint = dimensionAligned.SecondPoint ;
                dimensionLinear.ExtLineRotation = dimensionAligned.ExtLineRotation;
            }
        }

        internal static void ClearDimensionProperties(Dimension dimension)
        {
            dimension.Handle = default;
            dimension.Owner = default;
            dimension.XDictionary = default;
            dimension.Color = default;
            dimension.LineWeight = default;
            dimension.LinetypeScale = default;
            dimension.IsInvisible = default;
            dimension.Transparency = default;
            dimension.Version = default;
            dimension.DefinitionPoint = default;
            dimension.TextMiddlePoint = default;
            dimension.InsertionPoint = default;
            dimension.Normal = default;
            dimension.IsTextUserDefinedLocation = default;
            dimension.AttachmentPoint = default;
            dimension.LineSpacingStyle = default;
            dimension.LineSpacingFactor = default;
            dimension.Text = default;
            dimension.TextRotation = default;
            dimension.HorizontalDirection = default;

            if(dimension is DimensionAligned dimensionAligned)
            {
                dimensionAligned.FirstPoint = default;
                dimensionAligned.SecondPoint = default;
                dimensionAligned.ExtLineRotation = default;
            }
        }


        internal class DimensionPlaceholder:Dimension
        {
            public override ObjectType ObjectType { get { return ObjectType.INVALID; } }

            public override double Measurement { get; }

            internal DimensionPlaceholder() : base(DimensionType.Linear) { }
        }
    }
}
