using ACadSharp.Attributes;
using CSMath;
using System.Collections.Generic;

namespace ACadSharp.Entities
{
	public partial class Hatch
	{
		public partial class BoundaryPath
		{
			public class Polyline : Edge
			{
				/// <inheritdoc/>
				public override EdgeType Type => EdgeType.Polyline;

				/// <summary>
				/// The polyline has bulges with value different than 0.
				/// </summary>
				[DxfCodeValue(72)]
				public bool HasBulge
				{
					get
					{
						foreach (var v in this.Vertices)
						{
							if (v.Z != 0)
								return true;
						}
						return false;
					}
				}

				/// <summary>
				/// Is closed flag.
				/// </summary>
				[DxfCodeValue(73)]
				public bool IsClosed { get; set; }

				/// <summary>
				/// Position values are only X and Y.
				/// </summary>
				/// <remarks>
				/// The vertex bulge is stored in the Z component.
				/// </remarks>
				[DxfCodeValue(DxfReferenceType.Count, 93)]
				public List<XYZ> Vertices { get; set; } = new();

				/// <inheritdoc/>
				public BoundingBox GetBoundingBox()
				{
					return BoundingBox.FromPoints(this.Vertices);
				}
			}
		}
	}
}
