# Code quality review

Reviewed 2026-08-10. Scope: production C# under `src/`, with supporting checks against `tests/` and `Tools/` where the shared lint policy applies.

## Lint baseline

The repository now runs two complementary analyzer sets during every build:

- .NET SDK analyzers at `AnalysisMode=All` and `AnalysisLevel=latest`.
- SonarAnalyzer.CSharp 10.31.0.145097 for cognitive complexity, control-flow, maintainability, and correctness rules that the SDK analyzers do not cover.

`.editorconfig` adds the repository policy:

- Cognitive complexity: Sonar `S3776`, limit 15.
- Method length: Sonar `S138`, limit 80 lines.
- Cyclomatic complexity: `CA1502`, limit 15.
- Maintainability index: `CA1505`, minimum 20.
- Type coupling: `CA1506`, limit 15.
- Declarative loop and LINQ simplification: `S3267` and `S2971`.
- Immutability: `IDE0044`, `IDE0251`, readonly struct preferences, and `CA1815`.
- Naming rules for interfaces, types, members, constants, static fields, instance fields, parameters, and locals.
- Broad reliability, security, usage, maintainability, naming, and performance analysis. Lower-value API-design, documentation, localization, and cosmetic suggestions remain available in the IDE without dominating build output.

Run the explicit lint task with:

```sh
mise run lint
```

The existing `mise run build` and `mise run test` paths also run the analyzers because their configuration is in `Directory.Build.props`.

Baseline result: **71 warnings, 0 errors, successful build**. Warnings are non-blocking so the findings below can be accepted or rejected deliberately. The largest groups are 18 `CA1515` access-surface warnings, 13 `CA2007` async-context warnings, and 6 `S6966` async-I/O warnings. The requested review areas produced the more specific diagnostics cited below.

## Prioritized findings

| ID | Priority | Area | Summary |
|---|---|---|---|
| F1 | High | Naming, immutability | Replace the mutable, publicly exposed `Loc` sequence model. |
| F2 | High | Naming, primitive obsession | Stop using `Sietch` for every kind of map location and replace string-based location kinds. |
| F3 | High | Naming, immutability | Remove the two meanings of `Occupation` and the duplicated mutable occupation state. |
| F4 | High | Primitive obsession | Introduce value types for location and troop identity. |
| F5 | Medium | Functional style, complexity | Turn `DuneSavegame` construction into named parsing transformations. |
| F6 | Medium | Immutability, complexity | Represent the active selection as one state value instead of parallel nullable fields. |
| F7 | Medium | Cognitive complexity | Split HSQ instruction decoding without replacing its performance-sensitive loops. |
| F8 | Medium | Primitive obsession, naming | Model map coordinates and troop placement explicitly. |
| F9 | Medium | Naming | Name marker collections and rank properties after what they contain and mean. |
| F10 | Low | Cognitive complexity | Extract palette-entry parsing from `DecodeRgb24FromResource`. |
| F11 | Low | Control flow | Use a cursor-driven `while` loop for variable-width savegame instructions. |

## Detailed findings

### F1 — Replace the mutable, publicly exposed `Loc` sequence model

**Evidence**

- `src/DuneEdit.Core/LocSequences.cs:3-14` declares mutable `struct Loc`, public mutable fields `v1`, `v2`, and `v3`, and lowercase properties `region` and `subregion`.
- `src/DuneEdit.Core/LocSequences.cs:22` and `:78` expose mutable arrays as public `static readonly` fields named `compressed` and `uncompressed`. `readonly` protects only the array reference; any caller can replace elements or mutate each `Loc`.
- `src/DuneEdit.Core/DuneSavegame.cs:110-115` consumes those arrays when selecting a save format.
- `src/DuneEdit.Core/DuneSavegame.cs:173-201` relies on the three unnamed bytes to locate the location block.
- The lint baseline reports `IDE1006`, `CA1051`, `S1104`, and `CA1815` for this type.

**Risk**

The names do not communicate whether the bytes are IDs, sentinels, or record markers. A caller can mutate global parsing signatures and make subsequent loads fail process-wide. Because `Loc` has mutable value semantics and no equality implementation, it is also a poor dictionary, set, or test value.

**Recommendation**

Replace it with an immutable semantic value, for example:

```csharp
public readonly record struct LocationSignature(
    byte RegionId,
    byte SubregionId,
    byte Terminator);
```

Name the two collections `CompressedSaveSignatures` and `ExecutableSignatures`. Expose `ReadOnlySpan<LocationSignature>` if the data remains array-backed, or use `ImmutableArray<LocationSignature>` when callers need to retain the collection. Keep the on-disk bytes in this boundary type; do not leak `v1`/`v2`/`v3` into parsing code.

### F2 — Use one accurate location vocabulary and a typed location kind

**Evidence**

- `src/DuneEdit.Core/Sietch.cs:11` names the domain class `Sietch`.
- `src/DuneEdit.Core/Sietch.cs:60-68` shows that instances can be a sietch, Carthag palace, village, fort, Arrakeen palace, or an unknown type.
- `src/DuneEdit.Core/Sietch.cs:36` exposes the raw type code as `byte LocationType`.
- `src/DuneEdit.Core/Sietch.cs:60-75` converts the code to strings and then switches on those strings to derive a title.
- `src/DuneEdit.Desktop/ViewModels/LocationMarkerViewModel.cs:25-26` uses the string as both an image-cache key and a control-flow value.

**Risk**

`Sietch` is false for several valid instances, while `LocationTypeGroup` is not a group object but an asset-oriented string. A spelling change can silently break image lookup or control flow. Raw byte values, domain kinds, display text, and asset names are currently mixed in one API.

**Recommendation**

Rename `Sietch` to `DuneLocation` or `LocationRecord`. Introduce a `LocationKind` enum such as `Sietch`, `Village`, `Fort`, `CarthagPalace`, `ArrakeenPalace`, and `Unknown`. Keep the exact raw type code as an internal serialization property when it must round-trip. Derive display titles and asset names with exhaustive switch expressions at the UI boundary.

### F3 — Give `Occupation` one meaning and keep one immutable edited state

**Evidence**

- `src/DuneEdit.Core/FremenTroop.cs:22` names a raw encoded byte `Occupation`.
- `src/DuneEdit.Core/FremenTroop.cs:24` decodes it into `OccupationInfo`.
- `src/DuneEdit.Core/TroopOccupationInfo.cs:35-40` already provides a `readonly record struct` whose `Occupation` property is the semantic `TroopOccupation` enum and whose `RawJobCode` is the encoded byte.
- `src/DuneEdit.Desktop/ViewModels/FremenTroopDetailsViewModel.cs:17-21` stores `occupationInfo`, `selectedOccupation`, `selectedJob`, `selectedAllegiance`, and `jobCompleted` as five mutable fields for one logical value.
- `src/DuneEdit.Desktop/ViewModels/FremenTroopDetailsViewModel.cs:143-155` rebuilds the record and writes it back to the troop after each transition.

**Risk**

`Occupation` means both an encoded job/state byte and a semantic occupation depending on the containing type. The view model can temporarily hold combinations that disagree with `occupationInfo` or the troop. Each setter must manually maintain the invariant and issue the correct notifications.

**Recommendation**

Rename or hide the encoded property as `RawJobCode`. Make `TroopOccupationInfo` the single edited state in the view model. Implement pure transition functions such as `WithOccupation`, `WithJob`, and `WithAllegiance` that return a valid new record, including dependent defaults. Apply that record to the troop once per transition and derive the selected properties from it.

This keeps the existing immutable record and removes four parallel sources of truth. It also centralizes the rule that only valid occupation/job/allegiance combinations can be encoded.

### F4 — Introduce value types for location and troop identity

**Evidence**

- `src/DuneEdit.Core/DuneSavegame.cs:16` keys locations with `(byte Region, byte Subregion)`.
- `src/DuneEdit.Core/DuneSavegame.cs:122-129` accepts unrelated `byte` parameters for location and troop lookups.
- `src/DuneEdit.Core/Sietch.cs:28-29` exposes region and subregion identity as independently mutable bytes.
- `src/DuneEdit.Core/Sietch.cs:37` and `src/DuneEdit.Core/FremenTroop.cs:19-20` use plain bytes for linked troop IDs, including zero as a sentinel.

**Risk**

The compiler cannot prevent swapping region and subregion, passing a spice-field ID as a troop ID, or creating a partially changed location identity. Sentinel handling is distributed across loops instead of represented by the type.

**Recommendation**

Add narrowly scoped immutable value types:

```csharp
public readonly record struct LocationId(byte RegionId, byte SubregionId);
public readonly record struct TroopId(byte Value);
```

Use `LocationId` as the dictionary key and lookup argument. Model the zero troop sentinel explicitly at the serialization boundary, returning `TroopId?` or a `TryGetNextTroopId` result to domain code. Do not wrap every byte in the save format; wrap values that represent identity, carry invariants, or are easy to mix accidentally.

### F5 — Turn `DuneSavegame` construction into named parsing transformations

**Evidence**

- `src/DuneEdit.Core/DuneSavegame.cs:18-74` is a 57-line constructor that parses locations, builds an identity index, parses troops until a sentinel, and walks troop chains to build another index.
- `src/DuneEdit.Core/DuneSavegame.cs:30-39` imperatively creates both a list and a dictionary from the same records.
- `src/DuneEdit.Core/DuneSavegame.cs:44-57` imperatively projects fixed-size records and stops at the first empty slot.

**Risk**

The constructor describes storage mechanics before it communicates its three outcomes: parsed locations, parsed troops, and indexes. The mutation makes each phase harder to test independently and makes the constructor responsible for every invariant.

**Recommendation**

Extract named, mostly pure helpers:

- `ParseLocations(data, offset, signatures)` returns an immutable/read-only location collection.
- `ParseFremenTroops(data, offset)` returns records through the first empty slot.
- `IndexTroopLocations(locations, troops)` returns the lookup dictionary.

Inside the first two helpers, a projection such as `Enumerable.Range(...).Select(...)` expresses “parse every fixed-size record” more directly than a manually synchronized index, offset, list, and dictionary. The data sets are small and parsed only when opening a file, so the iterator overhead is unlikely to matter. If measurement shows otherwise, keep indexed loops inside the pure helpers; the important improvement is returning complete values instead of mutating constructor state.

Keep the chain walk at `DuneSavegame.cs:62-70` imperative. It is a bounded graph traversal with cycle detection, and a LINQ rewrite would obscure the termination rules.

### F6 — Represent selection as one state value

**Evidence**

- `src/DuneEdit.Desktop/ViewModels/MainViewModel.cs:12-14` stores the document, selected location marker, and selected troop marker separately.
- `src/DuneEdit.Desktop/ViewModels/MainViewModel.cs:26-29` separately stores two detail-view-model selections.
- `src/DuneEdit.Desktop/ViewModels/MainViewModel.cs:186-248` has two similar methods that clear the opposite selection, update marker flags, create details, and choose status text.
- Sonar reports nested conditional expressions at `MainViewModel.cs:213` and `:246`.

**Risk**

The valid state is “none, one location, or one troop,” but the representation permits both marker fields and both detail fields to be non-null. The two transition methods manually preserve that invariant through ordered mutations.

**Recommendation**

Represent selection with one immutable discriminated state, for example a small sealed record hierarchy or a `Selection` record containing exactly one typed target. Derive `SelectedLocation`, `SelectedFremenTroop`, `SelectedName`, `SelectedType`, and controller flags from that value. Route both click handlers through one transition method that unselects the previous marker and selects the next marker.

This is a functional state transition rather than a sequence of corrections to parallel mutable fields. It also removes the duplicated nested status expression.

### F7 — Split HSQ instruction decoding, but keep the decoding loops

**Evidence**

- Sonar reports cognitive complexity **24**, above the configured limit of 15, for `src/DuneEdit.Core/HsqCompression.cs:9-77`.
- The method tracks `source`, `destination`, and `control` as parallel integer state and contains literal, short back-reference, long back-reference, extended-count, terminator, and bounds-check branches.
- `src/DuneEdit.Core/HsqCompression.cs:72-75` copies a back-reference byte by byte.

**Risk**

Malformed-input checks and instruction decoding are interleaved, so a change to one HSQ instruction can affect cursor movement or termination elsewhere in the method. Parallel `ref int` state makes those dependencies implicit.

**Recommendation**

Encapsulate cursor and control-word state in a private `ref struct` reader. Extract one method that decodes the next instruction into a small readonly instruction value such as `Literal`, `BackReference`, or `End`. Let the outer loop apply the instruction and enforce destination bounds.

Keep the outer `while` and the byte-by-byte back-reference copy. This is a hot binary decoder, and overlapping LZ-style copies depend on progressive writes. A LINQ pipeline would allocate and would hide the state-machine semantics.

### F8 — Model map position and troop placement explicitly

**Evidence**

- `src/DuneEdit.Core/Sietch.cs:31-35` exposes both `MapPosX`/`MapPosY` and `PosX`/`PosY` without describing their coordinate systems.
- `src/DuneEdit.Core/FremenTroop.cs:21` stores `PositionAroundLocation` as a byte.
- `src/DuneEdit.Desktop/ViewModels/FremenTroopMarkerViewModel.cs:86-97` interprets values 1 through 8 as eight placements.
- `LocationMarkerViewModel.cs:29-34` and `FremenTroopMarkerViewModel.cs:32-36` duplicate byte-to-map projection and the encoded-latitude adjustment.

**Risk**

Any byte can be passed as a placement or coordinate. The two marker types can drift when projection rules change. Names such as `PosX` do not identify units, origin, encoded range, or whether the value is a game-world or flat-map coordinate.

**Recommendation**

Introduce a `TroopPlacement` enum and a readonly `MapPosition` value with named encoded coordinates. Put the latitude decoding and flat-map projection in one `MapProjection` helper that returns an Avalonia-independent point value. Rename or document the second `PosX`/`PosY` pair according to the coordinate system once its meaning is known.

### F9 — Align collection and rank names with their contents

**Evidence**

- `src/DuneEdit.Desktop/ViewModels/MainViewModel.cs:45-46` names collections `Locations` and `FremenTroops`, but their element types are `LocationMarkerViewModel` and `FremenTroopMarkerViewModel`.
- `MainViewModel.cs:13` uses generic `selectedMarker` next to the specific `selectedFremenTroopMarker`.
- `src/DuneEdit.Core/FremenTroop.cs:39` uses `ArmyRank`, while the enum and UI use “Military” at `TroopOccupationInfo.cs:5` and `FremenTroopDetailsViewModel.cs:38`.
- `src/DuneEdit.Desktop/ViewModels/FremenTroopDetailsViewModel.cs:7` uses camel case for a private static readonly field while other private static readonly fields use Pascal case; `IDE1006` reports it.

**Risk**

Callers must inspect element types to learn what collections contain. “Army” and “Military” appear to be separate concepts even though the UI treats them as one. The selected-marker names imply an asymmetry that does not exist.

**Recommendation**

Use `LocationMarkers`, `FremenTroopMarkers`, `selectedLocationMarker`, `selectedFremenTroopMarker`, `MilitaryRank`, and `CanonicalOccupations`. Apply one vocabulary across raw models, semantic models, view models, and labels. Preserve game-specific spellings only when they identify an actual on-disk concept, and document that boundary.

### F10 — Extract palette-entry parsing

**Evidence**

- Sonar reports cognitive complexity **17**, above the limit of 15, for `src/DuneEdit.Core/CryoPaletteDecoder.cs:14-71`.
- The method validates the resource boundary, scans entries, recognizes a terminator, validates color ranges, expands VGA components, and writes the destination palette.

**Risk**

The method is not exceptionally long, but entry framing and component validation are nested inside stream termination logic. Additional palette formats or diagnostics would increase nesting further.

**Recommendation**

Extract one `DecodeSubpalette` helper that consumes an entry at a cursor and writes its destination range. Keep the component loop: it is allocation-free, linear, and clearer than a LINQ projection with indexed writes. The outer method should own only boundary validation, termination, and the final “terminator found” invariant.

### F11 — Match savegame control flow to variable-width instructions

**Evidence**

- `src/DuneEdit.Core/SavegameCompression.cs:15-37` uses a `for` loop but advances `index` by two inside the body for an encoded run.
- Sonar `S127` reports the update of the loop control variable at line 36.

**Risk**

The loop header suggests one-byte iteration, while the body actually consumes one- or three-byte instructions. Future edits can easily advance the cursor twice or fail to advance it after a new branch.

**Recommendation**

Use an explicit cursor-driven `while (index < data.Length)` loop. Advance by one after a literal and by three after a marker sequence. A functional pipeline is not appropriate because instruction boundaries depend on previously consumed bytes; the `while` loop is the simpler representation.

## Loop audit

The analyzer baseline contains no `S3267` or `S2971` findings. Manual review reached the same conclusion for most loops: they are binary parsers, raster writers, fixed-buffer copies, graph walks, or UI mutations. Replacing them with LINQ would add iterators or hide side effects without making the operation clearer.

Keep these loops:

- Compression, decompression, palette, terrain, map-zone, and pixel loops in `DuneEdit.Core` and `MapFilterImageRenderer`.
- The troop-chain walk with cycle detection in `DuneSavegame`.
- Marker visibility updates in `MainViewModel`; they intentionally mutate observable UI objects.
- Visual-parent traversal in `MainWindow.IsOverButton`; the loop is shorter and cheaper than an ancestor enumerable.

The best functional opportunities are value-producing initialization phases, especially `DuneSavegame` parsing, and state transitions, especially occupation editing and selection. Functional style should stop at the mutation boundary rather than replacing loops with side-effecting `ForEach` calls.

## Function-length assessment

No method triggered the configured 80-line `S138` limit. Two methods exceeded the cognitive-complexity limit: `HsqCompression.Decompress` at 24 and `CryoPaletteDecoder.DecodeRgb24FromResource` at 17. The 57-line `DuneSavegame` constructor remains worth splitting because it performs several parsing phases even though its individual branches are shallow.

Large switch expressions in `TroopOccupationInfo` are data mappings rather than deeply nested control flow. Replacing them with runtime dictionaries would add storage, initialization, and lookup overhead without improving the small closed mapping. Keep the exhaustive switch form; split display-name extensions into another file only if file navigation becomes a problem.

## Immutability boundary

`TroopOccupationInfo` is already a good immutable domain value. `MapZones.Cells` also exposes its backing array safely through `ReadOnlySpan<byte>`.

`Sietch` and `FremenTroop` are mutable because editing is their purpose. Converting them wholesale to immutable records would require replacing object instances throughout the view-model graph after every edit. That change may be worthwhile later, but it is larger than the concrete risks above. First make identities, signatures, placement, occupation state, and selection immutable. Keep mutable save-record aggregates as an explicit exception with private byte storage and controlled setters.

## Suggested adoption order

1. Fix F1 first. It removes public global mutability and establishes the naming/readonly pattern.
2. Address F3 and F6 next. Both replace parallel mutable state with one valid state value.
3. Address F2 and F4 together because renaming the location model is the least disruptive time to introduce `LocationKind` and `LocationId`.
4. Extract the parsing phases in F5, then lower the measured complexity in F7 and F10.
5. Apply F8, F9, and F11 as contained cleanups.
