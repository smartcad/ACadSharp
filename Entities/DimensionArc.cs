using ACadSharp.Attributes;
using CSMath;

namespace ACadSharp.Entities
{
	/// <summary>
	/// Arc symbol type for arc length dimensions.
	/// </summary>
	public enum ArcLengthSymbolType
	{
		/// <summary>
		/// Arc symbol precedes the dimension text.
		/// </summary>
		Preceding = 0,

		/// <summary>
		/// Arc symbol is placed above the dimension text.
		/// </summary>
		Above = 1,

		/// <summary>
		/// No arc symbol is displayed.
		/// </summary>
		None = 2
	}

	/// <summary>
	/// Represents a <see cref="DimensionArc"/> entity (Arc Length Dimension).
	/// </summary>
	/// <remarks>
	/// Object name <see cref="DxfFileToken.EntityArcDimension"/> <br/>
	/// Dxf class name <see cref="DxfSubclassMarker.ArcDimension"/>
	/// </remarks>
	[DxfName(DxfFileToken.EntityArcDimension)]
	[DxfSubClass(DxfSubclassMarker.ArcDimension)]
	public class DimensionArc : Dimension
	{
		/// <inheritdoc/>
		public override ObjectType ObjectType => ObjectType.UNLISTED;

		/// <inheritdoc/>
		public override string ObjectName => DxfFileToken.EntityArcDimension;

		/// <inheritdoc/>
		public override string SubclassMarker => DxfSubclassMarker.ArcDimension;

		/// <summary>
		/// Start point for the first extension line (in WCS).
		/// Also known as XLine1Point.
		/// </summary>
		[DxfCodeValue(13, 23, 33)]
		public XYZ StartExtensionPoint { get; set; }

		/// <summary>
		/// Start point for the second extension line (in WCS).
		/// Also known as XLine2Point.
		/// </summary>
		[DxfCodeValue(14, 24, 34)]
		public XYZ EndExtensionPoint { get; set; }

		/// <summary>
		/// Center point of the arc being dimensioned (in WCS).
		/// </summary>
		[DxfCodeValue(15, 25, 35)]
		public XYZ CenterPoint { get; set; }

		/// <summary>
		/// Type of symbol to use in the arc length dimension's text string.
		/// </summary>
		/// <remarks>
		/// Note: This uses group code 70 in the AcDbArcDimension subclass,
		/// distinct from the base dimension flags code 70.
		/// </remarks>
		public ArcLengthSymbolType ArcSymbolType { get; set; }

		/// <summary>
		/// Leader length (arc radius for the dimension arc).
		/// </summary>
		[DxfCodeValue(40)]
		public double EndAngle { get; set; }

		/// <summary>
		/// Start extension line angle (in radians).
		/// Also known as ArcStartParam.
		/// </summary>
		[DxfCodeValue(41)]
		public double StartAngle { get; set; }

		/// <summary>
		/// Determines whether the arc length dimension has an extra leader drawn to resolve ambiguity.
		/// </summary>
		public bool HasLeader { get; set; }

		/// <summary>
		/// Start point for the arc length dimension's extra leader, if drawn.
		/// Also known as Leader1Point.
		/// </summary>
		[DxfCodeValue(16, 26, 36)]
		public XYZ Leader1Point { get; set; }

		/// <summary>
		/// End point for the arc length dimension's extra leader, if drawn.
		/// Also known as Leader2Point.
		/// </summary>
		[DxfCodeValue(17, 27, 37)]
		public XYZ Leader2Point { get; set; }

		/// <inheritdoc/>
		public override double Measurement
		{
			get
			{
				// Calculate arc length from the center point and extension points
				double radius = this.CenterPoint.DistanceFrom(this.StartExtensionPoint);
				XYZ startVec = this.StartExtensionPoint - this.CenterPoint;
				XYZ endVec = this.EndExtensionPoint - this.CenterPoint;

				// Calculate the angle between the two vectors
				double dot = startVec.X * endVec.X + startVec.Y * endVec.Y + startVec.Z * endVec.Z;
				double startLen = startVec.GetLength();
				double endLen = endVec.GetLength();

				if (startLen == 0 || endLen == 0)
					return 0;

				double cosAngle = dot / (startLen * endLen);
				cosAngle = System.Math.Max(-1.0, System.Math.Min(1.0, cosAngle));
				double angle = System.Math.Acos(cosAngle);

				return radius * angle;
			}
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public DimensionArc() : base(DimensionType.ArcLength) { }

		/// <inheritdoc/>
		public BoundingBox GetBoundingBox()
		{
			return BoundingBox.FromPoints(new[] {
				this.DefinitionPoint,
				this.StartExtensionPoint,
				this.EndExtensionPoint,
				this.CenterPoint,
				this.Leader1Point,
				this.Leader2Point
			});
		}
	}
}
