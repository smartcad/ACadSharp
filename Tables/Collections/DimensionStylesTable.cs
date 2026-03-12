using ACadSharp.IO.Templates;

namespace ACadSharp.Tables.Collections
{
	public class DimensionStylesTable : Table<DimensionStyle>
	{
		/// <inheritdoc/>
		public override ObjectType ObjectType => ObjectType.DIMSTYLE_CONTROL_OBJ;

		/// <inheritdoc/>
		public override string ObjectName => DxfFileToken.TableDimstyle;

		protected override string[] defaultEntries { get { return new string[] { DimensionStyle.DefaultName }; } }

		protected override DimensionStyle CreateDefaultEntry(string name) => new DimensionStyle(name);

		internal DimensionStylesTable() : base() { }

		internal DimensionStylesTable(CadDocument document) : base(document) { }
	}
}