namespace ACadSharp.Tables.Collections
{
	public class AppIdsTable : Table<AppId>
	{
		/// <inheritdoc/>
		public override ObjectType ObjectType => ObjectType.APPID_CONTROL_OBJ;

		/// <inheritdoc/>
		public override string ObjectName => DxfFileToken.TableAppId;

		protected override string[] defaultEntries { get { return new string[] { AppId.DefaultName }; } }

		protected override AppId CreateDefaultEntry(string name) => new AppId(name);

		internal AppIdsTable() : base() { }

		internal AppIdsTable(CadDocument document) : base(document) { }
	}
}