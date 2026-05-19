# ACadSharp / ACadSharp.Generators — Memory Optimization Plan

Scope: `ACadSharp/**` and `ACadSharp.Generators/**`.

Implementation status, 2026-05-20: **P0 and P1 are now implemented in code.** P2/P3 remain planned.

Savings estimates assume a representative drawing of ~100k `CadObject`s, ~30k entities, and a 10–50 MB DWG/DXF round-trip. They are order-of-magnitude — actual numbers vary with content. Estimates split into:
- **Steady** — long‑lived heap retained while a `CadDocument` is in memory.
- **Transient** — short-lived allocations (GC pressure) per read or write.

---

## P0 — Correctness bugs with memory impact

These are *real defects*. They also cause unbounded growth or pool starvation.

### 1. ✅ `ArrayPool<byte>.Shared.Rent` called twice instead of `Return`
File: [ACadSharp/IO/DWG/DwgStreamReaders/DwgObjectReader.cs](ACadSharp/IO/DWG/DwgStreamReaders/DwgObjectReader.cs#L1454-L1456)
```csharp
var arr = ArrayPool<byte>.Shared.Rent(dataSize);
this._objectReader.ReadBytes(arr, dataSize);
ArrayPool<byte>.Shared.Rent(dataSize);   // ← BUG: should be Return(arr)
```
- Effect: leaks the rented buffer on every annotated MText attribute. Pool will grow and never shrink for the rented size class.
- Fix: `ArrayPool<byte>.Shared.Return(arr);`
- **Estimated savings:** prevents `dataSize`-sized buffer leaks, typically 32 B – 64 KB per attribute. With many annotated attributes, can leak tens of MB into the shared pool over a session.

### 2. ✅ `MemoryStream.GetBuffer()` written without `Length`
File: [ACadSharp/IO/DWG/DwgStreamWriters/DwgObjectWriter.Objects.cs](ACadSharp/IO/DWG/DwgStreamWriters/DwgObjectWriter.Objects.cs#L763-L766)
```csharp
this._writer.WriteBitLong((int)ms.Length);
this._writer.WriteBytes(stream.GetBuffer());   // ← writes capacity, not ms.Length
```
- Effect: writes the underlying buffer capacity (usually a power of two ≥ length) of garbage to the output. Output bloat per XRecord and likely corrupt DWG.
- Fix: `this._writer.WriteBytes(stream.GetBuffer(), 0, (int)stream.Length);`
- **Estimated savings:** correctness + per-XRecord output reduction proportional to slack capacity (typically 1.5–2× the actual XRecord data).

### 3. ✅ Shared collections after `MemberwiseClone()` — `Clear()` mutates the original
After `MemberwiseClone`, reference fields (including get-only collections initialized in field initializers) point to the *same* instance. Calling `Clear()` then `Add(…)` on the clone empties the source and duplicates entries.

Confirmed instances:
- [ACadSharp/CadObject.cs](ACadSharp/CadObject.cs#L112-L123) — `clone.Reactors.Clear();` and `clone.ExtendedData.Clear();` clear the original's collections. This is the base used by *every* `Clone()` in the model.
- [ACadSharp/Entities/MLine.cs](ACadSharp/Entities/MLine.cs#L98-L107) — `clone.Vertices.Clear();`
- [ACadSharp/Entities/MLine.Vertex.cs](ACadSharp/Entities/MLine.Vertex.cs#L35-L46) — `clone.Segments.Clear();`
- [ACadSharp/Entities/HatchGradientPattern.cs](ACadSharp/Entities/HatchGradientPattern.cs#L73-L82) — `clone.Colors.Clear();`
- [ACadSharp/Objects/MultiLeaderAnnotContextClasses.cs](ACadSharp/Objects/MultiLeaderAnnotContextClasses.cs#L83-L95) — appends to shared `BreakStartEndPointsPairs` / `Lines` without reassigning.
- [ACadSharp/Objects/MultiLeaderAnnotContextClasses.cs](ACadSharp/Objects/MultiLeaderAnnotContextClasses.cs#L213-L225) — same pattern for `Points` and `StartEndPoints`.
- [ACadSharp/Objects/MultiLeaderAnnotContext.cs](ACadSharp/Objects/MultiLeaderAnnotContext.cs#L549-L558) — `clone.LeaderRoots.Clear();`
- [ACadSharp/Entities/MultiLeader.cs](ACadSharp/Entities/MultiLeader.cs#L578-L588) — `clone.BlockAttributes.Clear();`

Fix pattern (per class): assign a fresh collection on the clone *before* iterating the source:
```csharp
var clone = (T)MemberwiseClone();
clone.<Coll> = new List<...>();           // for settable collections
// or: replace the get-only init with a backing field you can reassign, then Clear() on a NEW list.
foreach (var item in this.<Coll>) clone.<Coll>.Add(item.Clone());
```
- **Estimated savings:**
  - Correctness first; prevents silently destroying the source's `Reactors` / `ExtendedData` / vertices on `Clone()`.
  - Stops unbounded duplicate growth on repeated `Clone()`. For long-running services that clone, this can prevent leaks scaling linearly with clone count (potentially **hundreds of MB**).

---

## P1 — High-impact allocation hotspots

### 4. ✅ `DxfMap.tryGetFromCache` rebuilds a full copy on every cache hit
File: [ACadSharp/DxfMap.cs](ACadSharp/DxfMap.cs#L66-L86)
- Allocates a new `DxfMap`, copies all `DxfProperties`, and copies all `SubClasses` dictionaries per call. Hit on every entity/object serialize and deserialize.
- Fix: cache the canonical map and return it directly. The map is structurally immutable after generation. If callers ever mutate, expose a `Clone()` explicitly.
- **Estimated savings (transient):** ~0.5–2 KB per cache hit × N entities. For 100k objects round-tripped: **~100–200 MB transient** dropped to near zero. Direct GC pressure win on read/write.

### 5. ✅ `DxfPropertyBase.DxfCodes` allocates `int[]` per access
File: [ACadSharp/DxfPropertyBase.cs](ACadSharp/DxfPropertyBase.cs#L25-L37)
- `DxfCodes` runs `Select((int)c).ToArray()` every getter call. `AssignedCode` calls it; consumers loop over it (e.g., [DxfHeaderSectionWriter.cs](ACadSharp/IO/DXF/DxfStreamWriter/DxfHeaderSectionWriter.cs#L49), [DxfReader.cs](ACadSharp/IO/DXF/DxfReader.cs#L203-L211), [DxfProperty.cs](ACadSharp/DxfProperty.cs#L65)).
- Fix: compute `int[]` once in the constructor and store it; or expose `ReadOnlySpan<int>`.
- **Estimated savings (transient):** ~24–64 B per call. With thousands of property hits per header/entity write: **a few MB transient** plus measurable cycle savings.

### 6. ✅ Generated `GetHeaderMap()` returns a full dictionary copy every call
File: [ACadSharp.Generators/DxfSourceGenerator.cs](ACadSharp.Generators/DxfSourceGenerator.cs#L450-L555)
```csharp
if (_headerMap != null) return new Dictionary<string, CadSystemVariable>(_headerMap);
...
return new Dictionary<string, CadSystemVariable>(map);
```
Called from [CadHeader.GetHeaderMap()](ACadSharp/Header/CadHeader.cs#L2946-L2950), [DxfReader.cs](ACadSharp/IO/DXF/DxfReader.cs#L184), [DxfHeaderSectionWriter.cs](ACadSharp/IO/DXF/DxfStreamWriter/DxfHeaderSectionWriter.cs#L22), [DxfWriterConfiguration.cs](ACadSharp/IO/DXF/DxfWriterConfiguration.cs#L93).
- ~150–300 entries; ~10–25 KB per copy.
- Fix: return the cached dictionary directly (or expose an `IReadOnlyDictionary<string, CadSystemVariable>` and switch internal callers).
- **Estimated savings (transient):** ~10–25 KB × N invocations. Modest in absolute terms; trivial to implement.

### 7. ✅ `CadObject.Reactors` and `ExtendedData` eagerly allocated for every object
File: [ACadSharp/CadObject.cs](ACadSharp/CadObject.cs#L66-L73)
- Every `CadObject` allocates a `Dictionary<ulong, CadObject>` (~72 B empty on .NET) and an `ExtendedDataDictionary` (~72 B + inner dict ~72 B), even when unused.
- Fix: lazy initialize, or use shared empty singletons until first mutation.
- **Estimated savings (steady):** ~200–250 B per `CadObject`. For 100k objects: **~20–25 MB steady** removed from a loaded document.

### 8. ✅ `CadObject.Clone()` always creates a new `CadDictionary` for `XDictionary`
File: [ACadSharp/CadObject.cs](ACadSharp/CadObject.cs#L112-L123)
- `clone.XDictionary = new CadDictionary();` runs even when the source has no extended dictionary. The setter then attempts `Document.RegisterCollection`.
- Fix: only assign when `this._xdictionary != null`; otherwise reset clone's backing field to null.
- **Estimated savings (steady):** ~300–600 B per `Clone()` saved when no XDictionary exists. Multiplies by clone volume.

### 9. ✅ `StreamIO.Write<T>(T)` allocates a fresh converter and calls `ToArray()`
File: [ACadSharp/CSUtilities/CSUtilities/IO/StreamIO.cs](ACadSharp/CSUtilities/CSUtilities/IO/StreamIO.cs#L443-L455)
```csharp
public void Write<T>(T value) where T : struct
{
    this.Write(value, new DefaultEndianConverter());   // alloc 1
}
...
byte[] arr = converter.GetBytes(value).ToArray();      // alloc 2 (ReadOnlySpan -> array)
```
- Hit per primitive write in DWG writing path.
- Fix: keep a static `DefaultEndianConverter` instance, write directly from the typed `GetBytes(short/int/long/...)` overloads which already return `byte[]`, or use a pooled 16-byte buffer + `BinaryPrimitives`/`BitConverter.TryWriteBytes`.
- **Estimated savings (transient):** 2 allocations of ~16–48 B per primitive write. DWG writes do this thousands of times — **5–50 MB transient** on a large drawing write.

### 10. ✅ `DxfSourceGenerator` builds a `Dictionary` then reverses through LINQ
File: [ACadSharp.Generators/DxfSourceGenerator.cs](ACadSharp.Generators/DxfSourceGenerator.cs#L308-L317)
```csharp
sb.AppendLine("            map.SubClasses = new Dictionary<string, DxfClassMap>(");
sb.AppendLine("                System.Linq.Enumerable.ToDictionary(");
sb.AppendLine("                    System.Linq.Enumerable.Reverse(map.SubClasses),");
sb.AppendLine("                    kvp => kvp.Key, kvp => kvp.Value));");
```
- Generated code reverses the dictionary via LINQ on every map build. Combined with Finding #4, this runs per cache hit today.
- Fix: emit subclass insertions in their final order instead of building-then-reversing.
- **Estimated savings (transient):** ~1–5 KB per map build × N. Tied to #4; both should be fixed together.

---

## P2 — Medium impact

### 11. DWG section read buffer churn
File: [ACadSharp/IO/DWG/DwgReader.cs](ACadSharp/IO/DWG/DwgReader.cs#L1025-L1158)
- AC18/AC21 section readers allocate one full-section `MemoryStream` plus per-page compressed and decompressed `byte[]`. Peak ≈ 2–3× section size.
- Fix: decompress directly into the destination buffer (`new byte[totalLength]`) using offsets; pool the per-page compressed buffers via `ArrayPool<byte>.Shared`.
- **Estimated savings (transient peak):** **20–40% of peak read RAM** during section load (multi-MB to tens of MB for large DWGs).

### 12. DWG writer stages each section in its own `MemoryStream`
File: [ACadSharp/IO/DWG/DwgWriter.cs](ACadSharp/IO/DWG/DwgWriter.cs#L176-L401) (`writeHeader`, `writeObjects`, `writeHandles`, `writeAuxHeader`, etc.)
- All section streams are alive simultaneously until file finalization.
- Fix: release section streams as they are committed; use pooled/recyclable streams (e.g., `RecyclableMemoryStream`-style).
- **Estimated savings (transient peak):** **10–25%** of peak write RAM.

### 13. `DwgFileHeaderWriterAC18.applyCompression` double-buffers with padding
File: [ACadSharp/IO/DWG/DwgStreamWriters/DwgFileHeaderWriterAC18.cs](ACadSharp/IO/DWG/DwgStreamWriters/DwgFileHeaderWriterAC18.cs#L138-L165)
- For every local section: a `holder` `MemoryStream` of `decompressedSize`, then a second `stream` for the compressed output.
- Fix: compress from the slice + virtual zero padding (loop in the compressor) without a temporary buffer.
- **Estimated savings (transient peak):** ~`decompressedSize` per local section, summed: **several MB** for a typical save.

### 14. `CadDictionary.EntryNames` / `EntryHandles` allocate arrays per access
File: [ACadSharp/Objects/CadDictionary.cs](ACadSharp/Objects/CadDictionary.cs#L130-L139)
- Both are DXF-mapped properties → invoked during serialization for every dictionary.
- Fix: build on demand and cache with a version counter; invalidate on Add/Remove. Or expose `IReadOnlyCollection<>` and have writers iterate.
- **Estimated savings (transient):** few hundred B per dictionary per serialization → **1–5 MB transient** on document write.

### 15. `DwgObjectReader` keeps three handle-index structures alive
File: [ACadSharp/IO/DWG/DwgStreamReaders/DwgObjectReader.cs](ACadSharp/IO/DWG/DwgStreamReaders/DwgObjectReader.cs#L100-L150)
- `Queue<ulong> _handles` (copy of all handles), `Dictionary<ulong, long> _map`, `Dictionary<ulong, ObjectType> _readedObjects` (pre-sized to map.Count).
- Fix: replace the queue with iteration over `_map.Keys`; track read state via a `HashSet<ulong>` (~half the memory of a value-dictionary entry); or reuse `_map` itself by removing as read.
- **Estimated savings (transient):** **~16–24 B × handle count**. 100k handles ≈ **1.5–2 MB** during read.

### 16. `DwgObjectWriter` enumerates entities multiple times and materializes
File: [ACadSharp/IO/DWG/DwgStreamWriters/DwgObjectWriter.cs](ACadSharp/IO/DWG/DwgStreamWriters/DwgObjectWriter.cs#L213) and [DwgObjectWriter.Entities.cs](ACadSharp/IO/DWG/DwgStreamWriters/DwgObjectWriter.Entities.cs#L2421-L2431)
- `blkRecord.Entities.ToArray()`, plus `Count()` / `ElementAt(i)` in `writeChildEntities`. Backing storage is `HashSet<Entity>` → `Count()` is O(1) but `ElementAt(i)` is O(n) and `ToArray()` copies.
- Fix: single-pass iterator that tracks prev/next; or change `CadObjectCollection<T>` to an ordered `List<T>` so indexers are cheap, removing the need for `ToArray()`.
- **Estimated savings (transient):** **a few MB** on large drawings + CPU.

### 17. `DxfWriterConfiguration.RemoveHeaderVariable` LINQ-scans + lowercases per call
File: [ACadSharp/IO/DXF/DxfWriterConfiguration.cs](ACadSharp/IO/DXF/DxfWriterConfiguration.cs#L110-L116)
- Allocates ~`Variables.Length` strings per invocation.
- Fix: `private static readonly HashSet<string> _reserved = new(Variables, StringComparer.OrdinalIgnoreCase);` and check `_reserved.Contains(name)`.
- **Estimated savings (transient):** ~1–2 KB per call. Minor; trivial.

### 18. `CadUtils` LINQ in hot helpers
File: [ACadSharp/CadUtils.cs](ACadSharp/CadUtils.cs#L215-L230)
- `GetCodePage` uses `value.ToLower()` allocating a new string per call.
- `GetCodeIndex` does `_pageCodes.ToList().IndexOf(code)`.
- Fix: build `_dxfEncodingMap` with `StringComparer.OrdinalIgnoreCase` and use `Array.IndexOf(_pageCodes, code)`.
- **Estimated savings (transient):** small; few MB across many reads.

### 19. `Color` `R`/`G`/`B` accessors allocate via `BitConverter.GetBytes`
File: [ACadSharp/Color.cs](ACadSharp/Color.cs#L330-L340), [ACadSharp/Color.cs](ACadSharp/Color.cs#L530-L537)
- `getRGBfromTrueColor` calls `LittleEndianConverter.Instance.GetBytes(color)` → allocates a `byte[4]` per RGB access.
- Fix: compute components with bit shifts (`(byte)(color & 0xFF)`, `(byte)((color >> 8) & 0xFF)`, …) and remove the byte-array round-trip. Consider flattening `_indexRgb` from `byte[][]` to a single `byte[]` to drop ~256 small array headers.
- **Estimated savings:** ~16 B per RGB access (transient). Steady: ~3–5 KB from collapsing the jagged table. Color is read in many render/export paths.

---

## P3 — Lower-impact cleanups (still worth doing as a batch)

### 20. Attribute constructors use `Select().ToArray()`
- [ACadSharp/Attributes/DxfCodeValueAttribute.cs](ACadSharp/Attributes/DxfCodeValueAttribute.cs#L17)
- [ACadSharp/Attributes/DxfCollectionCodeValueAttribute.cs](ACadSharp/Attributes/DxfCollectionCodeValueAttribute.cs#L17)
- [ACadSharp/Attributes/CadSystemVariableAttribute.cs](ACadSharp/Attributes/CadSystemVariableAttribute.cs#L31)
- Fix: direct `for` loop or accept `DxfCode[]` overloads. Attributes are typically constructed once per type via reflection, so this is small; but the generator can prefer the `DxfCode[]` ctor.
- **Estimated savings:** negligible at runtime; minor startup win.

### 21. `IEnumerable<double> Bulges` re-creates the LINQ pipeline per access
File: [ACadSharp/Entities/Hatch.BoundaryPath.Polyline.cs](ACadSharp/Entities/Hatch.BoundaryPath.Polyline.cs#L21-L36)
- `HasBulge` enumerates `Bulges` (another `Select`).
- Fix: enumerate `Vertices` directly in `HasBulge`; document that `Bulges` is lazy.
- **Estimated savings:** small; cleanup.

### 22. `CadImageBase.GetBoundingBox` enumerates `ClipBoundaryVertices` four times
File: [ACadSharp/Entities/CadImageBase.cs](ACadSharp/Entities/CadImageBase.cs#L185-L210)
- Fix: single pass tracking min/max.
- **Estimated savings:** minor allocations + 4× CPU.

### 23. `StringExtensions.GetLines` / `ToByteArray` / `ToArgs` LINQ + concatenation
File: [ACadSharp/CSUtilities/CSUtilities/Extensions/StringExtensions.cs](ACadSharp/CSUtilities/CSUtilities/Extensions/StringExtensions.cs#L80-L115)
- `lines.Take(lines.Length - 1).ToArray()` instead of `Array.Resize` or new `string[length-1]`.
- `ToByteArray` builds via `Where/Select/ToArray`.
- `ToArgs` uses `word += c` in a loop (O(n²) string allocations).
- Fix: index-based loops; `StringBuilder` for accumulators; or `ReadOnlySpan<char>` slicing.
- **Estimated savings:** depends on usage frequency. Significant if used in DXF parsing hot paths.

### 24. `ReflectionExtensions.GetPropertyByName` and enum `*ByStringValue` are uncached
Files: [ReflectionExtensions.cs](ACadSharp/CSUtilities/CSUtilities/Extensions/ReflectionExtensions.cs#L9-L12), [EnumExtensions.cs](ACadSharp/CSUtilities/CSUtilities/Extensions/EnumExtensions.cs#L74-L147)
- `Type.GetProperties()` and `Type.GetFields()` each allocate a fresh array.
- Fix: cache by `Type` in a `ConcurrentDictionary` if used in any runtime path.
- **Estimated savings:** path-dependent; small unless invoked in tight loops.

### 25. `ByteExtensions.ToHexString` calls `array.Count()` on `IEnumerable<byte>`
File: [ACadSharp/CSUtilities/CSUtilities/Extensions/ByteExtensions.cs](ACadSharp/CSUtilities/CSUtilities/Extensions/ByteExtensions.cs#L15-L21)
- `array.Count()` enumerates the sequence once just for sizing — fine if backing is a `byte[]`, expensive otherwise. Currently no callers in-tree, so low priority.

### 26. `DwgFileHeaderWriterAC15.AddSection` keeps `(record, MemoryStream)` tuples
File: [ACadSharp/IO/DWG/DwgStreamWriters/DwgFileHeaderWriterAC15.cs](ACadSharp/IO/DWG/DwgStreamWriters/DwgFileHeaderWriterAC15.cs#L49-L150)
- Each section's `MemoryStream` is retained in the dictionary until flush.
- Fix: stream sections out as they arrive (or release tuples after writing).
- **Estimated savings (transient peak):** **MB-range** for AC14/AC15 writes.

---

## Aggregate impact summary

| Bucket | Steady (per loaded 100k-object doc) | Transient (per large read/write) |
|---|---|---|
| P0 (correctness/leaks) | — | prevents unbounded growth; pool leak fix |
| P1 (#4, #5, #6, #7, #8, #9, #10) | **~20–30 MB** (mostly #7) | **~120–250 MB** (mostly #4 + #9) |
| P2 (#11–#19) | ~few MB | **~20–60 MB** + lower peak |
| P3 (#20–#26) | <1 MB | small |

Conservative end-state target: **~25 MB lower steady** memory per loaded document and **~150–300 MB less transient GC pressure** per read/write of a large drawing, plus elimination of the clone-correctness defects and the pool-leak defect.

---

## Suggested implementation order

1. **P0**: #1, #2, #3 — bug fixes first; #3 unblocks safe `Clone()` paths.
2. **P1 quick wins**: #4 (one method, big delta), #5 (constructor change), #6 (one-line generator change), #7 (lazy fields), #8 (one `if`), #10 (generator emit order).
3. **P1 medium**: #9 (StreamIO write rework — touches many call sites).
4. **P2**: #11–#13 (DWG section memory shape), #14–#16 (collection access patterns), then #17–#19.
5. **P3**: opportunistic.

## Out of scope (verified acceptable)

- `Color` is a `readonly struct` — boxing is the dominant concern, not field size.
- Source generator `StringBuilder(64 * 1024)` capacity at [DxfSourceGenerator.cs](ACadSharp.Generators/DxfSourceGenerator.cs#L112) is appropriate for a single compile.
- `DxfMap._cache` / `DxfClassMap._cache` `ConcurrentDictionary` usage is fine — the problem is what `tryGetFromCache` does *with* the hit (Finding #4).
- `DwgObjectReader` constructor's `TryGetBuffer` short-circuit (already implemented) is a good pattern; just complete the related fixes in #11 and #15.
