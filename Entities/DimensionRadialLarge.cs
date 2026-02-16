using ACadSharp.Attributes;
using CSMath;

namespace ACadSharp.Entities
{
	/// <summary>
	/// Represents a <see cref="DimensionRadialLarge"/> entity (Jogged Radius Dimension).
	/// </summary>
	/// <remarks>
	/// Object name <see cref="DxfFileToken.EntityLargeRadialDimension"/> <br/>
	/// Dxf class name <see cref="DxfSubclassMarker.RadialDimensionLarge"/>
	/// </remarks>
	[DxfName(DxfFileToken.EntityLargeRadialDimension)]
	[DxfSubClass(DxfSubclassMarker.RadialDimensionLarge)]
	public class DimensionRadialLarge : Dimension
	{
		/// <inheritdoc/>
		public override ObjectType ObjectType => ObjectType.UNLISTED;

		/// <inheritdoc/>
		public override string ObjectName => DxfFileToken.EntityLargeRadialDimension;

		/// <inheritdoc/>
		public override string SubclassMarker => DxfSubclassMarker.RadialDimensionLarge;

		/// <summary>
		/// Chord point on the arc being dimensioned (in WCS).
		/// This is where the dimension line touches the arc.
		/// </summary>
		[DxfCodeValue(13, 23, 33)]
		public XYZ ChordPoint { get; set; }

		/// <summary>
		/// Override center point used by the jogged radius dimension (in WCS).
		/// This allows repositioning the apparent center for visual clarity.
		/// </summary>
		[DxfCodeValue(14, 24, 34)]
		public XYZ OverrideCenter { get; set; }

		/// <summary>
		/// Jog point used by the jogged radius dimension (in WCS).
		/// This is where the jog (bend) occurs in the dimension line.
		/// </summary>
		[DxfCodeValue(15, 25, 35)]
		public XYZ JogPoint { get; set; }

		/// <summary>
		/// Jog angle used by the jogged radius dimension (in radians).
		/// This defines the angle of the jog symbol.
		/// </summary>
		[DxfCodeValue(40)]
		public double JogAngle { get; set; }

		/// <summary>
		/// Gets the center point of the arc being dimensioned.
		/// This is the same as the DefinitionPoint for radial dimensions.
		/// </summary>
		public XYZ CenterPoint => this.DefinitionPoint;

		/// <inheritdoc/>
		public override double Measurement
		{
			get
			{
				return this.DefinitionPoint.DistanceFrom(this.ChordPoint);
			}
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public DimensionRadialLarge() : base(DimensionType.Radius) { }

		/// <inheritdoc/>
		public BoundingBox GetBoundingBox()
		{
			return BoundingBox.FromPoints(new[] { this.DefinitionPoint, this.ChordPoint, this.OverrideCenter, this.JogPoint });
		}
	}
}
