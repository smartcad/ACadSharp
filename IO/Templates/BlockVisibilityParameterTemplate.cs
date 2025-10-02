using System.Collections.Generic;

using ACadSharp.Entities;
using ACadSharp.Objects;
using ACadSharp.Objects.Evaluations;

namespace ACadSharp.IO.Templates {

	internal class BlockVisibilityParameterTemplate : CadTemplate<BlockVisibilityParameter> {

		public BlockVisibilityParameterTemplate(BlockVisibilityParameter cadObject)
			: base(cadObject) {
		}

        public IList<ulong> TotalEntityHandles { get; } = new List<ulong>();

        public IDictionary<BlockVisibilityParameter.SubBlock, IList<ulong>> SubBlockHandles { get; } = new Dictionary<BlockVisibilityParameter.SubBlock, IList<ulong>>();

		public override void Build(CadDocumentBuilder builder) {
			base.Build(builder);

			IDictionary<ulong, Entity> TotalEntityHandleMaps = new Dictionary<ulong, Entity>();

			foreach (ulong handle in this.TotalEntityHandles) {
				if (builder.TryGetCadObject(handle, out Entity entity)) {
                    TotalEntityHandleMaps[handle] = entity;
					this.CadObject.Entities.Add(entity);
				}
			}

			foreach (var subGroup in this.CadObject.SubBlocks) {
				if (this.SubBlockHandles.TryGetValue(subGroup, out IList<ulong> subBlockHandles)) {
					foreach (ulong handle in subBlockHandles) {
						if (TotalEntityHandleMaps.TryGetValue(handle, out Entity entity)) {
							subGroup.Entities.Add(entity);
						}
						else if (builder.TryGetCadObject(handle, out Entity entityX)) {
						}
					}
				}
			}
		}
	}
}