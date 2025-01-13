namespace ACadSharp.Tables.Collections
{
	public class LayersTable : Table<Layer>
	{
		/// <inheritdoc/>
		public override ObjectType ObjectType => ObjectType.LAYER_CONTROL_OBJ;

		/// <inheritdoc/>
		public override string ObjectName => DxfFileToken.TableLayer;

		protected override string[] defaultEntries { get { return System.Array.Empty<string>(); } }

		internal LayersTable() { }

		internal LayersTable(CadDocument document) : base(document) { }
	}
}