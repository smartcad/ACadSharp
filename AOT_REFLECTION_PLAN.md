# ACadSharp AOT / Reflection Removal Plan

## Recommendation

The best path is to keep the existing attributes as the authoring surface, but stop reading them at runtime.

Use a build-time generator to emit static metadata and factories for:

- DXF type maps
- DXF property accessors
- header variable metadata
- header value factories
- table and table-entry constructors
- subclass-marker-to-type lookups used by DXF readers

This removes the .NET Native / AOT-sensitive runtime reflection without forcing a big manual rewrite of all entity classes.

The inventory in DXF_PROPERTY_INVENTORY.txt shows the scale of the metadata surface today:

- 160 DxfName/DxfSubClass markers
- 819 DxfCodeValue mappings
- 229 CadSystemVariable mappings

That is large enough that a manual registry will drift. A generator is the right default.

## Why Not Use A Text File At Runtime

A text file is useful as an audit artifact, but not as the runtime source of truth.

Problems with a runtime text file:

- it becomes a second schema to maintain
- it still requires parsing and validation logic
- it does not give typed getters, setters, or constructors by itself
- it is easy for the file and the C# model to diverge

Use the text file only for review, diffing, and migration tracking. Generate code from source, not runtime metadata from text.

## Target Architecture

### 1. Generated metadata registry

Add a source generator project that scans ACadSharp source for:

- DxfNameAttribute
- DxfSubClassAttribute
- DxfCodeValueAttribute
- DxfCollectionCodeValueAttribute
- CadSystemVariableAttribute

Emit generated code similar to:

```csharp
internal static partial class DxfMetadataRegistry
{
    public static DxfMapInfo GetMap(Type type);
    public static DxfClassMapInfo GetSubclass(string subclassMarker);
    public static HeaderVariableInfo GetHeaderVariable(string name);
}

internal sealed class DxfPropertyAccessor
{
    public int[] Codes { get; init; }
    public int[] CollectionCodes { get; init; }
    public DxfReferenceType ReferenceType { get; init; }
    public Func<CadObject, object> Getter { get; init; }
    public Action<CadObject, int, object> Setter { get; init; }
}
```

Important point: the generator should emit direct property access lambdas or direct methods, not PropertyInfo access.

### 2. Replace DXF map reflection with registry lookup

Replace runtime map building in:

- DxfMapBase
- DxfMap
- DxfClassMap

New behavior:

- DxfMap.Create(Type) becomes a lookup into generated metadata
- DxfClassMap.Create<T>() becomes a lookup into generated metadata
- no GetProperties
- no GetCustomAttribute
- no runtime property enumeration

The current cache can stay, but it should cache generated descriptors rather than reflection-built objects.

### 3. Replace PropertyInfo.GetValue / SetValue

The main AOT-sensitive hot path is not only metadata discovery, but also property access inside DxfPropertyBase.

Refactor DxfPropertyBase so it no longer stores PropertyInfo. Store a generated accessor payload instead:

- property type info known at build time
- getter delegate
- setter delegate
- collection-code metadata

Keep the existing DXF conversion logic for:

- XY / XYZ partial component assignment
- Color
- PaperMargin
- Transparency
- enum conversion
- handle / name / count semantics

Only replace the reflective property access part.

### 4. Remove header reflection completely

CadHeader still has more reflection than just constructor activation. The full replacement should cover:

- GetHeaderMap()
- SetValue(string, params object[])
- GetValue(string)
- GetValues(string)
- PropertyExpression<CadHeader, CadSystemVariableAttribute>

Generated header metadata should provide:

- variable name
- DXF codes
- reference flags
- getter
- setter
- optional factory for complex target types

That removes:

- GetProperties
- GetCustomAttribute
- Expression.Compile
- runtime constructor lookup
- Activator.CreateInstance in header assignment

### 5. Replace Activator.CreateInstance with explicit factories

There are only a few runtime construction patterns, which is good news.

Replace them with generated or handwritten registries:

#### Header value factories

For CadHeader.SetValue, generate per-property creation logic instead of looking up constructors dynamically.

Examples:

- primitive: direct Convert
- enum: Enum.ToObject or generated cast path
- DateTime: CadUtils.FromJulianCalendar
- TimeSpan: CadUtils.EditingTime
- structured value objects: generated factory lambda for the exact property type

#### Table collection factories

Replace Activator.CreateInstance(typeof(T)) in CadDocumentBuilder.registerTable with a static registry:

```csharp
internal static class TableFactoryRegistry
{
    public static T CreateTable<T>() where T : ITable;
}
```

This can be generated from the known Table<> subclasses or written by hand because the set is small.

#### Default table-entry factories

Replace Activator.CreateInstance(typeof(T), new object[] { entry }) in Table<T>.CreateDefaultEntries with either:

- a generated registry keyed by entry type, or
- a new abstract factory hook on Table<T>

The cleanest option is:

```csharp
protected abstract T CreateDefaultEntry(string name);
```

Each concrete table already knows its T. That avoids reflection entirely and keeps the code simple.

### 6. Pre-register subclass variants used during DXF reading

Some subtype maps are added lazily while reading, especially:

- dimensions
- polyline variants
- vertex variants

Do not keep building these on demand with DxfClassMap.Create<T>().

Instead, generate static registries for:

- subclass marker -> DxfClassMapInfo
- subclass marker -> object factory

That lets DxfSectionReaderBase select the right concrete type without reflection and without mutating map metadata at runtime.

### 7. Keep public APIs stable where possible

Keep these entry points if you want low external churn:

- DxfMap.Create<T>()
- DxfClassMap.Create<T>()
- CadHeader.GetHeaderMap()

Internally, have them forward to generated registries.

That limits the migration mostly to implementation internals.

## Rollout Plan

### Phase 0: Freeze the current metadata surface

- Keep DXF_PROPERTY_INVENTORY.txt checked in as an audit artifact.
- Add golden tests for representative entities, tables, and header variables.
- Add at least one trim / AOT validation build to CI if possible.

### Phase 1: Eliminate Activator first

This is the smallest, safest first win.

- Replace CadDocumentBuilder.registerTable activation.
- Replace Table<T>.CreateDefaultEntries activation.
- Replace CadHeader.SetValue constructor lookup and activation.

This immediately reduces constructor metadata requirements under AOT.

### Phase 2: Replace header metadata path

Generate header variable descriptors first.

Why first:

- the surface is large but conceptually simple
- the call sites are concentrated in CadHeader and DXF header readers/writers
- it removes both reflection and expression compilation in one pass

### Phase 3: Replace DXF map building

Generate DxfMap / DxfClassMap descriptors from source attributes.

At the end of this phase, DxfMapBase, DxfMap, and DxfClassMap should no longer enumerate properties or read custom attributes at runtime.

### Phase 4: Replace DxfPropertyBase reflective access

Swap PropertyInfo-backed access for generated accessors.

This is the highest-value runtime change after metadata generation, because it removes PropertyInfo.GetValue / SetValue from the read/write hot path.

### Phase 5: Remove reflection fallback paths

Once generated paths are validated:

- delete PropertyExpression if nothing else uses it
- delete reflection-based map builders
- delete runtime attribute scanning in CadHeader
- keep attributes only as generator inputs

## Concrete Implementation Notes

### Source generator scope

Because ACadSharp targets netstandard2.0, put the generator in a separate analyzer package/project. The main library can stay on netstandard2.0.

### Metadata model to generate

Generate plain arrays and dictionaries, not deeply dynamic builders.

Prefer structures like:

- Type -> map descriptor
- string DXF name -> object factory
- string subclass marker -> subclass descriptor
- string system variable name -> header descriptor
- int DXF code -> property accessor arrays

### Avoid runtime Type switching where a string key already exists

In several DXF reader paths, the subclass marker string is already the discriminator. Use that directly for lookup instead of re-deriving metadata from Type.

### Do not forget DxfCollectionCodeValueAttribute

DxfProperty.GetCollectionCodes() still reads metadata reflectively today. If the main map path is generated but collection codes are left reflective, the migration is incomplete.

## Best Option And Fallbacks

### Best option

Incremental source generator that emits:

- metadata registries
- property accessors
- header variable registries
- constructor factories

This gives the best AOT story and the least long-term maintenance.

### Simpler fallback

If you want a lower-risk first step before introducing a generator, hand-write two small registries first:

- constructor factories for tables, default entries, and header structured values
- header variable registry

Then add the generator for DXF map metadata second.

### Worst option

Do not make a manually maintained runtime text manifest the primary metadata source. It adds maintenance cost without solving typed access cleanly.

## Definition Of Done

The migration is complete when ACadSharp can:

- read and write DXF without GetProperties / GetCustomAttribute / PropertyInfo.GetValue / PropertyInfo.SetValue in the DXF/header map path
- set header values without runtime constructor discovery
- create tables and default entries without Activator.CreateInstance
- run without Expression.Compile in the metadata path
- pass a representative AOT / trimmed build validation