using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;

namespace ACadSharp.IO.Templates
{
	internal class CadDimensionTemplate : CadEntityTemplate
	{
		public ulong? StyleHandle { get; set; }

		public ulong? BlockHandle { get; set; }

		public string BlockName { get; set; }

		public string StyleName { get; set; }

		public CadDimensionTemplate() : base(new DimensionPlaceholder()) { }

		public CadDimensionTemplate(Dimension dimension) : base(dimension) { }

		public override void Build(CadDocumentBuilder builder)
		{
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

		public class DimensionPlaceholder : Dimension
		{
			public override ObjectType ObjectType { get { return ObjectType.INVALID; } }

			public override double Measurement { get; }

			public DimensionPlaceholder() : base(DimensionType.Linear) { }
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

		public void SetDimensionObject(Dimension new_dim)
		{
			new_dim.Handle = this.CadObject.Handle;
			new_dim.Owner = this.CadObject.Owner;

			new_dim.XDictionary = this.CadObject.XDictionary;
			//dimensionAligned.Reactors = this.CadObject.Reactors;
			//dimensionAligned.ExtendedData = this.CadObject.ExtendedData;

			new_dim.Color = this.CadObject.Color;
			new_dim.LineWeight = this.CadObject.LineWeight;
			new_dim.LinetypeScale = this.CadObject.LinetypeScale;
			new_dim.IsInvisible = this.CadObject.IsInvisible;
			new_dim.Transparency = this.CadObject.Transparency;

			Dimension dimension = this.CadObject as Dimension;

			new_dim.Version = dimension.Version;
			new_dim.DefinitionPoint = dimension.DefinitionPoint;
			new_dim.TextMiddlePoint = dimension.TextMiddlePoint;
			new_dim.InsertionPoint = dimension.InsertionPoint;
			new_dim.Normal = dimension.Normal;
			new_dim.IsTextUserDefinedLocation = dimension.IsTextUserDefinedLocation;
			new_dim.AttachmentPoint = dimension.AttachmentPoint;
			new_dim.LineSpacingStyle = dimension.LineSpacingStyle;
			new_dim.LineSpacingFactor = dimension.LineSpacingFactor;
			//dimensionAligned.Measurement = dimension.Measurement;
			new_dim.Text = dimension.Text;
			new_dim.TextRotation = dimension.TextRotation;
			new_dim.HorizontalDirection = dimension.HorizontalDirection;

			if(dimension is DimensionAligned dim_aligned && new_dim is DimensionLinear dim_lin)
			{
				dim_lin.FirstPoint = dim_aligned.FirstPoint;
				dim_lin.SecondPoint = dim_aligned.SecondPoint;
				dim_lin.ExtLineRotation = dim_lin.ExtLineRotation;
			}

			this.CadObject = new_dim;
		}
	}
}
