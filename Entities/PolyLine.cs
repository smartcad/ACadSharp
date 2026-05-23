using ACadSharp.Attributes;
using CSMath;
using CSUtilities.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace ACadSharp.Entities
{
	/// <summary>
	/// Represents a <see cref="Polyline"/> entity.
	/// </summary>
	[DxfName(DxfFileToken.EntityPolyline)]
	[DxfSubClass(null, true)]
	public class Polyline : Entity, IPolyline
	{
		/// <inheritdoc/>
		public override string ObjectName => DxfFileToken.EntityPolyline;


		private ObjectType _objectType = ObjectType.POLYLINE_2D;

        /// <inheritdoc/>
        public override ObjectType ObjectType => _objectType;

        /// <inheritdoc/>
        [DxfCodeValue(30)]
		public double Elevation { get; set; } = 0.0;

		/// <inheritdoc/>
		[DxfCodeValue(39)]
		public double Thickness { get; set; } = 0.0;

		/// <inheritdoc/>
		[DxfCodeValue(210, 220, 230)]
		public XYZ Normal { get; set; } = XYZ.AxisZ;

		/// <summary>
		/// Polyline flags.
		/// </summary>
		[DxfCodeValue(70)]
		public PolylineFlags Flags { get; set; }

		/// <summary>
		/// Start width.
		/// </summary>
		[DxfCodeValue(40)]
		public double StartWidth { get; set; } = 0.0;

		/// <summary>
		/// End width.
		/// </summary>
		[DxfCodeValue(41)]
		public double EndWidth { get; set; } = 0.0;

		//71	Polygon mesh M vertex count(optional; default = 0)
		//72	Polygon mesh N vertex count(optional; default = 0)
		//73	Smooth surface M density(optional; default = 0)
		//74	Smooth surface N density(optional; default = 0)

		/// <summary>
		/// Curves and smooth surface type.
		/// </summary>
		[DxfCodeValue(75)]
		public SmoothSurfaceType SmoothSurface { get; set; }

		/// <summary>
		/// Vertices that form this polyline.
		/// </summary>
		/// <remarks>
		/// Each <see cref="Vertex"/> has it's own unique handle.
		/// </remarks>
		public SeqendCollection<Vertex> Vertices { get; private set; }

		/// <inheritdoc/>
		public bool IsClosed
		{
			get
			{
				return this.Flags.HasFlag(PolylineFlags.ClosedPolylineOrClosedPolygonMeshInM) || this.Flags.HasFlag(PolylineFlags.ClosedPolygonMeshInN);
			}
			set
			{
				if (value)
				{
					this.Flags = this.Flags.AddFlag(PolylineFlags.ClosedPolylineOrClosedPolygonMeshInM);
					this.Flags = this.Flags.AddFlag(PolylineFlags.ClosedPolygonMeshInN);
				}
				else
				{
					this.Flags = this.Flags.RemoveFlag(PolylineFlags.ClosedPolylineOrClosedPolygonMeshInM);
					this.Flags = this.Flags.RemoveFlag(PolylineFlags.ClosedPolygonMeshInN);
				}
			}
		}

		/// <inheritdoc/>
		IEnumerable<IVertex> IPolyline.Vertices { get { return this.Vertices; } }

		public Polyline() : base()
		{
			this.Vertices = new SeqendCollection<Vertex>(this);
		}

		/// <inheritdoc/>
		public override CadObject Clone()
		{
			Polyline clone = (Polyline)base.Clone();

			clone.Vertices = new SeqendCollection<Vertex>(clone);
			foreach (Vertex v in this.Vertices)
			{
				clone.Vertices.Add((Vertex)v.Clone());
			}

			return clone;
		}

		internal override void AssignDocument(CadDocument doc)
		{
			base.AssignDocument(doc);
			doc.RegisterCollection(this.Vertices);
		}

		internal override void UnassignDocument()
		{
			this.Document.UnregisterCollection(this.Vertices);
			base.UnassignDocument();
		}
	}
}
