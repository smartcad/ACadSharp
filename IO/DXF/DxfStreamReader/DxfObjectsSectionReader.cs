using ACadSharp.IO.Templates;
using ACadSharp.Objects;
using CSMath;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ACadSharp.IO.DXF
{
	internal class DxfObjectsSectionReader : DxfSectionReaderBase
	{
		public delegate bool ReadObjectDelegate<T>(CadTemplate template, DxfMap map) where T : CadObject;

		public DxfObjectsSectionReader(IDxfStreamReader reader, DxfDocumentBuilder builder)
			: base(reader, builder)
		{
		}

		public override void Read()
		{
			//Advance to the first value in the section
			this._reader.ReadNext();

			//Loop until the section ends
			while (this._reader.ValueAsString != DxfFileToken.EndSection)
			{
				CadTemplate template = null;

				try
				{
					template = this.readObject();
				}
				catch (Exception ex)
				{
					if (!this._builder.Configuration.Failsafe)
						throw;

					this._builder.Notify($"Error while reading an object at line {this._reader.Position}", NotificationType.Error, ex);

					while (this._reader.DxfCode != DxfCode.Start)
						this._reader.ReadNext();
				}

				if (template == null)
					continue;

				//Add the object and the template to the builder
				this._builder.AddTemplate(template);
			}
		}

		private CadTemplate readObject()
		{
			switch (this._reader.ValueAsString)
			{
				case DxfFileToken.ObjectDictionary:
					return this.readObjectCodes<CadDictionary>(new CadDictionaryTemplate(), this.readDictionary);
				case DxfFileToken.ObjectDictionaryWithDefault:
					return this.readObjectCodes<CadDictionaryWithDefault>(new CadDictionaryWithDefaultTemplate(), this.readDictionaryWithDefault);
				case DxfFileToken.ObjectLayout:
					return this.readObjectCodes<Layout>(new CadLayoutTemplate(), this.readLayout);
				case DxfFileToken.ObjectDictionaryVar:
					return this.readObjectCodes<DictionaryVariable>(new CadTemplate<DictionaryVariable>(new DictionaryVariable()), this.readObjectSubclassMap);
				case DxfFileToken.ObjectPdfDefinition:
					return this.readObjectCodes<PdfUnderlayDefinition>(new CadNonGraphicalObjectTemplate(new PdfUnderlayDefinition()), this.readObjectSubclassMap);
				case DxfFileToken.ObjectSortEntsTable:
					return this.readSortentsTable();
				case DxfFileToken.ObjectDimAssoc:
					return this.readDimAssoc();
				case DxfFileToken.ObjectScale:
					return this.readObjectCodes<Scale>(new CadTemplate<Scale>(new Scale()), this.readScale);
				case DxfFileToken.ObjectVisualStyle:
					return this.readObjectCodes<VisualStyle>(new CadTemplate<VisualStyle>(new VisualStyle()), this.readVisualStyle);
				case DxfFileToken.ObjectXRecord:
					return this.readObjectCodes<XRecord>(new CadXRecordTemplate(), this.readXRecord);
				default:
					DxfMap map = DxfMap.Create<CadObject>();
					CadUnknownNonGraphicalObjectTemplate unknownEntityTemplate = null;
					if (this._builder.DocumentToBuild.Classes.TryGetByName(this._reader.ValueAsString, out Classes.DxfClass dxfClass))
					{
						this._builder.Notify($"NonGraphicalObject not supported read as an UnknownNonGraphicalObject: {this._reader.ValueAsString}", NotificationType.NotImplemented);
						unknownEntityTemplate = new CadUnknownNonGraphicalObjectTemplate(new UnknownNonGraphicalObject(dxfClass));
					}
					else
					{
						this._builder.Notify($"UnknownNonGraphicalObject not supported: {this._reader.ValueAsString}", NotificationType.NotImplemented);
					}

					this._reader.ReadNext();

					do
					{
						if (unknownEntityTemplate != null && this._builder.KeepUnknownEntities)
						{
							this.readCommonCodes(unknownEntityTemplate, out bool isExtendedData, map);
							if (isExtendedData)
								continue;
						}

						this._reader.ReadNext();
					}
					while (this._reader.DxfCode != DxfCode.Start);

					return unknownEntityTemplate;
			}
		}

		protected CadTemplate readObjectCodes<T>(CadTemplate template, ReadObjectDelegate<T> readEntity)
			where T : CadObject
		{
			this._reader.ReadNext();

			DxfMap map = DxfMap.Create<T>();

			while (this._reader.DxfCode != DxfCode.Start)
			{
				if (!readEntity(template, map))
				{
					this.readCommonCodes(template, out bool isExtendedData, map);
					if (isExtendedData)
						continue;
				}

				if (this._reader.DxfCode != DxfCode.Start)
					this._reader.ReadNext();
			}

			return template;
		}

		private bool readObjectSubclassMap(CadTemplate template, DxfMap map)
		{
			switch (this._reader.Code)
			{
				default:
					return this.tryAssignCurrentValue(template.CadObject, map.SubClasses[template.CadObject.SubclassMarker]);
			}
		}

		private bool readPlotSettings(CadTemplate template, DxfMap map)
		{
			switch (this._reader.Code)
			{
				default:
					return this.tryAssignCurrentValue(template.CadObject, map.SubClasses[DxfSubclassMarker.PlotSettings]);
			}
		}

		private bool readLayout(CadTemplate template, DxfMap map)
		{
			CadLayoutTemplate tmp = template as CadLayoutTemplate;

			switch (this._reader.Code)
			{
				case 330:
					tmp.PaperSpaceBlockHandle = this._reader.ValueAsHandle;
					return true;
				case 331:
					tmp.LasActiveViewportHandle = (this._reader.ValueAsHandle);
					return true;
				default:
					if (!this.tryAssignCurrentValue(template.CadObject, map.SubClasses[DxfSubclassMarker.Layout]))
					{
						return this.readPlotSettings(template, map);
					}
					return true;
			}
		}

		private bool readScale(CadTemplate template, DxfMap map)
		{
			switch (this._reader.Code)
			{
				// Undocumented codes
				case 70:
					//Always 0
					return true;
				default:
					return this.tryAssignCurrentValue(template.CadObject, map.SubClasses[DxfSubclassMarker.Scale]);
			}
		}

		private bool readVisualStyle(CadTemplate template, DxfMap map)
		{
			switch (this._reader.Code)
			{
				// Undocumented codes
				case 176:
				case 177:
				case 420:
					return true;
				default:
					return this.tryAssignCurrentValue(template.CadObject, map.SubClasses[DxfSubclassMarker.VisualStyle]);
			}
		}

		private bool readXRecord(CadTemplate template, DxfMap map)
		{
			CadXRecordTemplate tmp = template as CadXRecordTemplate;

			switch (this._reader.Code)
			{
				case 100 when this._reader.ValueAsString == DxfSubclassMarker.XRecord:
					this.readXRecordEntries(tmp.CadObject);
					return true;
				default:
					return this.tryAssignCurrentValue(template.CadObject, map.SubClasses[DxfSubclassMarker.XRecord]);
			}
		}

		private void readXRecordEntries(XRecord recrod)
		{
			this._reader.ReadNext();

			while (this._reader.DxfCode != DxfCode.Start)
			{
				recrod.CreateEntry(this._reader.Code, this._reader.Value);

				this._reader.ReadNext();
			}
		}

		private bool readDictionary(CadTemplate template, DxfMap map)
		{
			CadDictionary cadDictionary = new CadDictionary();
			CadDictionaryTemplate tmp = template as CadDictionaryTemplate;

			switch (this._reader.Code)
			{
				case 280:
					cadDictionary.HardOwnerFlag = this._reader.ValueAsBool;
					return true;
				case 281:
					cadDictionary.ClonningFlags = (DictionaryCloningFlags)this._reader.Value;
					return true;
				case 3:
					tmp.Entries.Add(this._reader.ValueAsString, null);
					return true;
				case 350: // Soft-owner ID/handle to entry object 
				case 360: // Hard-owner ID/handle to entry object
					tmp.Entries[tmp.Entries.LastOrDefault().Key] = this._reader.ValueAsHandle;
					return true;
				default:
					return this.tryAssignCurrentValue(template.CadObject, map.SubClasses[DxfSubclassMarker.Dictionary]);
			}
		}

		private bool readDictionaryWithDefault(CadTemplate template, DxfMap map)
		{
			CadDictionaryWithDefaultTemplate tmp = template as CadDictionaryWithDefaultTemplate;

			switch (this._reader.Code)
			{
				case 340:
					tmp.DefaultEntryHandle = this._reader.ValueAsHandle;
					return true;
				default:
					if (!this.tryAssignCurrentValue(template.CadObject, map.SubClasses[DxfSubclassMarker.DictionaryWithDefault]))
					{
						return this.readDictionary(template, map);
					}
					return true;
			}
		}

		private CadTemplate readDimAssoc()
		{
			DimensionAssociativity dimassoc = new DimensionAssociativity();
			DimAssocTemplate template = new DimAssocTemplate(dimassoc);

			//Jump the 0 marker
			this._reader.ReadNext();

			this.readCommonObjectData(template);

			System.Diagnostics.Debug.Assert(DxfSubclassMarker.DimAssoc == this._reader.ValueAsString);

			//Jump the 100 AcDbDimAssoc marker
			this._reader.ReadNext();

			List<ulong> geometryHandles = new List<ulong>();
			List<DimensionAssociativity.ObjectSnapPointReference> pointRefs = new List<DimensionAssociativity.ObjectSnapPointReference>();

			while (this._reader.DxfCode != DxfCode.Start)
			{
				switch (this._reader.Code)
				{
					// Dimension object handle (330 after subclass marker)
					case 330:
						template.DimensionHandle = this._reader.ValueAsHandle;
						break;
					// Associativity flag
					case 90:
						dimassoc.AssociativityFlag = (DimensionAssociativity.DimassocAssociativityPoint)this._reader.ValueAsShort;
						break;
					// Trans space flag
					case 70:
						dimassoc.TransSpaceFlag = this._reader.ValueAsBool;
						break;
					// Rotated dimension type
					case 71:
						dimassoc.RotatedDimensionFlag = (DimensionAssociativity.RotatedDimensionTypes)this._reader.ValueAsShort;
						break;
					// Class name - "AcDbOsnapPointRef" (start of a point reference)
					case 1:
						// Read the snap point reference fields
						this.readOsnapPointRef(geometryHandles, pointRefs);
						continue; // readOsnapPointRef advances the reader, don't ReadNext again
					default:
						this._builder.Notify($"Group Code not handled {this._reader.GroupCodeValue} for {typeof(DimensionAssociativity)}, code : {this._reader.Code} | value : {this._reader.ValueAsString}");
						break;
				}

				this._reader.ReadNext();
			}

			if (pointRefs.Count > 0)
			{
				dimassoc.PointRefs = pointRefs.ToArray();
				template.MainGeometryHandle = geometryHandles.ToArray();
			}

			return template;
		}

		private void readOsnapPointRef(List<ulong> geometryHandles, List<DimensionAssociativity.ObjectSnapPointReference> pointRefs)
		{
			// We're currently at code 1 = "AcDbOsnapPointRef", advance past it
			this._reader.ReadNext();

			DimensionAssociativity.ObjectOSnapTypes snapType = DimensionAssociativity.ObjectOSnapTypes.None;
			ulong geometryHandle = 0;
			short subentType = 0;
			int gsMarker = 0;
			double parameter = 0.0;
			XYZ point = XYZ.Zero;
			bool hasLastPointReference = false;

			while (this._reader.DxfCode != DxfCode.Start)
			{
				switch (this._reader.Code)
				{
					case 72:
						snapType = (DimensionAssociativity.ObjectOSnapTypes)this._reader.ValueAsShort;
						break;
					case 331:
						geometryHandle = this._reader.ValueAsHandle;
						break;
					case 73:
						subentType = this._reader.ValueAsShort;
						break;
					case 91:
						gsMarker = this._reader.ValueAsInt;
						break;
					case 40:
						parameter = this._reader.ValueAsDouble;
						break;
					case 10:
						point = new XYZ(this._reader.ValueAsDouble, point.Y, point.Z);
						break;
					case 20:
						point = new XYZ(point.X, this._reader.ValueAsDouble, point.Z);
						break;
					case 30:
						point = new XYZ(point.X, point.Y, this._reader.ValueAsDouble);
						break;
					case 75:
						hasLastPointReference = this._reader.ValueAsBool;
						break;
					case 1:
						// Next "AcDbOsnapPointRef" - save current and start new one
						geometryHandles.Add(geometryHandle);
						pointRefs.Add(new DimensionAssociativity.ObjectSnapPointReference(
							snapType, subentType, gsMarker, parameter, point, hasLastPointReference));

						// Reset for next point ref
						this.readOsnapPointRef(geometryHandles, pointRefs);
						return;
					default:
						this._builder.Notify($"Group Code not handled in OsnapPointRef, code : {this._reader.Code} | value : {this._reader.ValueAsString}");
						break;
				}

				this._reader.ReadNext();
			}

			// Save the last point reference
			geometryHandles.Add(geometryHandle);
			pointRefs.Add(new DimensionAssociativity.ObjectSnapPointReference(
				snapType, subentType, gsMarker, parameter, point, hasLastPointReference));
		}

		private CadTemplate readSortentsTable()
		{
			SortEntitiesTable sortTable = new SortEntitiesTable();
			CadSortensTableTemplate template = new CadSortensTableTemplate(sortTable);

			//Jump the 0 marker
			this._reader.ReadNext();

			this.readCommonObjectData(template);

			System.Diagnostics.Debug.Assert(DxfSubclassMarker.SortentsTable == this._reader.ValueAsString);

			//Jump the 100 marker
			this._reader.ReadNext();

			(ulong?, ulong?) pair = (null, null);

			while (this._reader.DxfCode != DxfCode.Start)
			{
				switch (this._reader.Code)
				{
					case 5:
						pair.Item1 = this._reader.ValueAsHandle;
						break;
					case 330:
						template.BlockOwnerHandle = this._reader.ValueAsHandle;
						break;
					case 331:
						pair.Item2 = this._reader.ValueAsHandle;
						break;
					default:
						this._builder.Notify($"Group Code not handled {this._reader.GroupCodeValue} for {typeof(SortEntitiesTable)}, code : {this._reader.Code} | value : {this._reader.ValueAsString}");
						break;
				}

				if (pair.Item1.HasValue && pair.Item2.HasValue)
				{
					template.Values.Add((pair.Item1.Value, pair.Item2.Value));
					pair = (null, null);
				}

				this._reader.ReadNext();
			}

			return template;
		}
	}
}
