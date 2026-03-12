namespace ACadSharp.Tables.Collections
{
	public class LayersTable : Table<Layer>
	{
		/// <inheritdoc/>
		public override ObjectType ObjectType => ObjectType.LAYER_CONTROL_OBJ;

		/// <inheritdoc/>
		public override string ObjectName => DxfFileToken.TableLayer;

		protected override string[] defaultEntries { get { return System.Array.Empty<string>(); } }

		protected override Layer CreateDefaultEntry(string name) => throw new System.NotSupportedException();

		internal LayersTable() { }

		internal LayersTable(CadDocument document) : base(document) { }
	}
}