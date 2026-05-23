using ACadSharp.Blocks;
using ACadSharp.Entities;
using ACadSharp.IO.DWG;
using ACadSharp.Objects;
using ACadSharp.Tables;
using CSUtilities.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace ACadSharp.IO.Templates
{
	internal class CadBlockRecordTemplate : CadTableEntryTemplate<BlockRecord>
	{
		public ulong? FirstEntityHandle { get; set; }

		public ulong? LastEntityHandle { get; set; }

		public ulong? BeginBlockHandle { get; set; }

		public ulong? EndBlockHandle { get; set; }

		public ulong? LayoutHandle { get; set; }

		public List<ulong> OwnedObjectsHandlers { get; set; } = new List<ulong>();
		public CadBlockRecordTemplate() : base(new BlockRecord()) { }

		public CadBlockRecordTemplate(BlockRecord block) : base(block) { }

		public override void Build(CadDocumentBuilder builder)
		{
			base.Build(builder);

			if (builder.TryGetCadObject(this.LayoutHandle, out Layout layout))
			{
				this.CadObject.Layout = layout;
			}

			if (this.FirstEntityHandle.HasValue)
			{
				IEnumerable<Entity> entityCollection;
				if((this.CadObject.Name == BlockRecord.ModelSpaceName || this.CadObject.Name == BlockRecord.ModelSpaceNameCap) && builder is DwgDocumentBuilder dwg_builder)
				{
					entityCollection = dwg_builder.ModelSpaceEntities.Where(ent => ent.Owner is null);
				}
                else
                {
					entityCollection = this.getEntitiesCollection<Entity>(builder, this.FirstEntityHandle.Value, this.LastEntityHandle.Value);
                }

				short viewPortId = 0;
                foreach (Entity e in entityCollection)
				{
					if(e is Viewport vp)
						viewPortId++;
                    this.addEntity(builder, e, viewPortId);
				}
			}
			else
            {
                short viewPortId = 0;
                foreach (ulong handle in this.OwnedObjectsHandlers)
				{
					if (builder.TryGetCadObject(handle, out Entity child))
                    {
                        if(child is Viewport vp)
                            viewPortId++;
                        this.addEntity(builder, child, viewPortId);
					}
				}
			}
		}

		public void SetBlockToRecord(CadDocumentBuilder builder)
		{
			if (builder.TryGetCadObject(this.BeginBlockHandle, out Block block))
			{
				if (!block.Name.IsNullOrEmpty())
				{
					this.CadObject.Name = block.Name;
				}

				block.Flags = this.CadObject.BlockEntity.Flags;
				block.BasePoint = this.CadObject.BlockEntity.BasePoint;
				block.XrefPath = this.CadObject.BlockEntity.XrefPath;
				block.Comments = this.CadObject.BlockEntity.Comments;

				this.CadObject.BlockEntity = block;
			}

			if (builder.TryGetCadObject(this.EndBlockHandle, out BlockEnd blockEnd))
			{
				this.CadObject.BlockEnd = blockEnd;
			}
		}

		private void addEntity(CadDocumentBuilder builder, Entity entity, short viewPortId)
		{
			if(!builder.KeepUnknownEntities && entity is UnknownEntity)
				return;

			if(entity is Viewport vp)
				vp.Id = viewPortId;

            if(this.CadObject.Entities.Contains(entity))
				return;
			this.CadObject.Entities.Add(entity);
		}
	}
}
