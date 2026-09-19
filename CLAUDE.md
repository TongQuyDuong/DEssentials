# DEssentials

A drop-in Unity utility library, consumed as a **git submodule** at `Assets/_DEssentials`
(origin: `github.com/TongQuyDuong/DEssentials`). Edits here belong to *this* repo — commit
inside the submodule first, then update the gitlink in the host project.

It is a grab-bag by design: ~85 files of small, independent helpers. Nothing here is a
framework, almost nothing depends on anything else here, and most files are usable in
isolation. Read this before assuming a subsystem exists — usually it doesn't, it's one file.

## How it compiles

**There are no `.asmdef` files.** Everything lands in `Assembly-CSharp`, and anything under
an `Editor/` folder lands in `Assembly-CSharp-Editor`. The one exception is
`Shaders/Editor/`, which carries an `.asmref` (`TMP_Editor_Ref.asmref`) that folds it into
TextMeshPro's own editor assembly so it can reach TMP's internal editor types.
Root namespace is `Dessentials.*`
(a handful of strays: `iGame.AllInSlime`, `Runtime.Dessentials.Features.Fix_Object_Names.Editor`,
and three files with no namespace at all — `Singleton.cs`, `RendererTextureAnimator.cs`, and
`SROptions.Dessentials.cs`, which is global on purpose because SRDebugger requires it).

Because there's no asmdef, **every optional third-party dependency is gated by a `#if` flag
instead of an assembly reference.** Without the right flag the code is invisible to the
compiler, so the library drops into a project with none of those packages and still builds.

## Compatibility flags

Set these in **Project Settings → Player → Scripting Define Symbols** (per build target).
The `DESSENTIALS_*` ones are this library's own and are never auto-defined — if a feature
seems missing, the flag is the first thing to check.

| Flag | Unlocks | Needs |
| --- | --- | --- |
| `DESSENTIALS_ZEGO_SDK` | The whole analytics/live-ops surface: `IFirebaseAnalytics`, `IGameInitializer`, `ISessionDataProvider`, `ITransitionalDataProvider`, `IRemoteConfigValueProvider`, and both Bamboo and Taichi trackers | Zego SDK (internal iKame registry) + Firebase Analytics |
| `DESSENTIALS_DOTWEEN` | Every DOTween path in the library: `SectionedProgressBar`, `GameObjectGridLayout2D`, `GameObjectHorizontalLayout`, `RestartableTweener`, `SpammableTweener`, `DTransformExtensions.DOBouncyScale` | DOTween |
| `DESSENTIALS_PRIME_TWEEN` | PrimeTween backend of `GameObjectHorizontalLayout` only | PrimeTween |
| `DESSENTIALS_RCORE` | `IgnoreSafeZone` (extends `RCore.UI`) | RCore |
| `DESSENTIALS_SRDEBUGGER` | `SROptions.Dessentials` debug-panel entries and `BambooTrackOptions` | SRDebugger |
| `DESSENTIALS_SPINE_ANIMATION` | `SpineAnimationEntity` | Spine Unity runtime |
| `DESSENTIALS_INIT_ARGS` | `IInitArgsService<T>` (wraps `Sisus.Init.Service`) | Init(args) by Sisus |
| `FIREBASE_CRASHLYTICS` | Bodies of `DFirebaseCrashlytics.Log` / `.LogException` — the class still exists without it, the calls just no-op | Firebase Crashlytics |
| `ODIN_INSPECTOR` | `[Button]`, `[FoldoutGroup]`, `[InlineProperty]` inspector polish throughout (51 sites) | Odin Inspector — **auto-defined by Odin**, don't set by hand |
| `UNITASK_DOTWEEN_SUPPORT` | `.ToUniTask()` on DOTween tweens in `GameObjectHorizontalLayout` | UniTask + DOTween — **auto-defined by UniTask** |
| `DESSENTIALS_DEBUG_LOG_IN_BUILD` | Not a package gate. Keeps the "X has not been registered!" `LogError` from `ServiceLocator` and `IInitArgsService` alive in player builds; otherwise it's editor-only | none |

**Migration note:** DOTween used to be gated by a bare `DOTWEEN` flag. That is gone —
`DESSENTIALS_DOTWEEN` is now the only one. A project that still defines `DOTWEEN` in its
scripting define symbols must rename it, or every DOTween path in this library silently
compiles out. Don't reintroduce unprefixed flags; all of this library's own gates carry
the `DESSENTIALS_` prefix so they can't collide with a package's own defines.

**The no-tween default.** `GameObjectHorizontalLayout` is the reference for how an optional
tween backend should be written: `#if DESSENTIALS_DOTWEEN` / `#elif DESSENTIALS_PRIME_TWEEN`
/ `#else`, where the `#else` branch snaps straight to the final value. Note the signature
changes — `RepositionSmooth()` returns `Sequence` under either tween flag and `void` under
neither, since no `Sequence` type exists then.

## What's in here

### `Common/` — the reusable core

**Service access.** Three unrelated mechanisms; pick one per project, don't mix.
- `ServiceLocator` — instance-based `Register<T>` / `Get<T>` / `TryGet<T>`.
- `IGlobalService<T>` — static `Register(service)` + `Exist`, logs an error on unregistered access.
- `IInitArgsService<T>` — thin wrapper over Sisus Init(args). Needs `DESSENTIALS_INIT_ARGS`.

**`EventBus`** — static, type-per-event. `EventBus<T>.Raise(evt)` with `EventBinding<T>`
handles registered via `Register`/`Deregister`. `IEvent` is the marker. `EventBusUtil`
does the reflection to find event types.

**Singletons** — four flavours: `Singleton<T>` (plain C#), `MonoSingleton<T>`,
`PersistentMonoSingleton<T>` (survives scene loads), `SingletonScriptableObject<T>`.

**Entity management** — `ManagedEntity` (with a `ManagedEntityState` lifecycle),
`ManagedEntityFactory`, `ManagedEntityRegistry`, plus an unmanaged `Registry`.
`IDisposableEntity` is the common contract.

**Global services** — interfaces only (`IFirebaseAnalytics`, `IGameInitializer`,
`ISessionDataProvider`, `ITransitionalDataProvider`) so the host game supplies the impls.
Mostly behind `DESSENTIALS_ZEGO_SDK`. `DFirebaseCrashlytics` is the one concrete class.

### `Common/UI/`

- `ScreenSafeZone` — notch/safe-area handling, plus static `SetTopOffsetForBannerAd` /
  `SetBottomOffsetForBannerAd` for ad-banner layouts. `IgnoreSafeZone` opts a child out
  (needs `DESSENTIALS_RCORE`).
- `ImprovedGridLayoutGroup` — a `GridLayoutGroup` subclass.
- `SectionedProgressBar` — segmented bar; animates when `DOTWEEN` is on.
- `TMP_CurveText` — bends TextMeshPro text along a curve.

### `Common/Utility/`

- `GameObjectHorizontalLayout` / `GameObjectGridLayout2D` — lay out **world-space child
  transforms** (not RectTransforms) in a row or grid. `RepositionNow()` snaps,
  `RepositionSmooth()` tweens.
- `RestartableTweener` / `SpammableTweener` — wrap a `Func<Tween>` so repeated
  `PlayTween()` calls behave (restart vs. overlap). `DOTWEEN` only.
- `OverrideableConfig<TKey, TConfig>` (`TKey : IConvertible`, `TConfig : class`) — a default
  config plus per-key overrides, with JSON import on both layers (`ImportDefaultFromJson`,
  `ImportOverride`, `ImportOverridesFromJson`, `HasOverride`, `GetOrCreateOverride`). The most
  substantial single helper here.
- `RoundRobinQueue<T>` — `Enqueue`/`Dequeue` that cycles rather than drains.
- `DBug` — `Log` / `LogWarning` / `LogError` / `LogException` indirection layer.
- `ExtendedParticleSystem`, `RendererTextureAnimator`, `LevelScaler2D`,
  `RectTransformPositionAnchor` — small MonoBehaviour utilities.
- `[AutoFill]` — attribute + drawer that auto-assigns a serialized reference by path.

### `Extensions/` — sparse, check before assuming

Despite six files, only a few extension methods exist: `DImageExtensions`
(`SetNativeAspectRatioByHeight/Width`), `DMathExtensions` (`FormatIntWithSpaceSeparator`),
`DTransformExtensions` (`DOBouncyScale`, DOTween-gated). `DArrayExtensions`,
`DListExtensions` and `DTypeExtensions` hold static helpers rather than `this`-extensions.

### `Helpers/`

- `DTimeHelper` — the richest one: unix timestamp conversion, `FormatDHMs`, `FormatHhMmSs`,
  `CalcSecondsPassed`, `CalcStepsPassed`, week/month boundary math.
- `DAssetHelper` — **editor-side** asset moves, GUID lookup, directory scans for
  prefabs/JSON, used/unused folder shuffling.
- `CSVHelper` — append/read/clear CSV in `Application.persistentDataPath`.

### `Serializables/` — make things show up in the Inspector

- `SerializableDictionary<TKey, TValue>` / `SerializedDictionary` + `SerializableKeyValue`.
- `InterfaceReference<TInterface, TObject>` — serialize an interface field; implicit
  conversion to the interface. Pair with `[RequireInterface(typeof(IFoo))]` on an
  `Object` field. Custom drawers included.
- `FolderReference` / `FolderCollection` — serialize a project folder path.
- `SortingLayerField` — sorting-layer dropdown.
- `EnumTypedGameObject` / `EnumTypedObject` / `ValueTypedGameObject` — enum-keyed pairs.

### `Features/` — larger, opt-in units

- **ABTesting** — `ABTest<T> : IABTest` with `Init`/`Fetch`/`GetGuaranteedValue`,
  `FirebaseKey` binding, `[RegisteredABTest]` discovery via
  `ABTestManager : SingletonScriptableObject<ABTestManager>`, remote values through
  `IRemoteConfigValueProvider`.
- **Tracking** — three independent trackers, all `DESSENTIALS_ZEGO_SDK`:
  `BambooTracker` (event classes: `ActionOccurenceAmountBambooEvent`,
  `ActionOccurenceAmountIntervalBambooEvent`, `HighValueUserBambooEvent`),
  `TaichiTracker`, and `TotalAndIncrementalAdRevenueTracker`. Data comes from the
  `_Services/` provider interfaces (`IRevenueDataProvider`, `IWatchAdsAmountProvider`,
  `IAlreadyTrackedEventsProvider`) that the host game implements.
- **LevelDatabase** — `LevelDatabase<TLeveScriptableObject> : ScriptableObject` (the typo in
  the type parameter is in the source): name→asset lookup with a fallback level, bulk
  `ImportLevelsByName` / `ImportBackupLevelsByName`, `RemoveNullEntries`.
- **QuickAccess** (editor) — `QuickAccessWindow` + config/groups for pinning assets.
- **Convert Json to Scriptable Objects** (editor) — window driven by `IImportFromJson`.
- **Fix Object Names** (editor) — renames an asset's main object to match its filename.
- **CustomizedInputModule**, **Spine** (`SpineAnimationEntity`, flag-gated).

### `Shaders/`

A TextMeshPro-derived shader (`TMP_Improved.shader`) and its `.cginc` includes
(`Dessentials_TMPro*.cginc`), plus the inspector GUI at `Editor/TMP_SDFShaderGUI.cs`.

`Editor/` here is the `.asmref` folder described above. An earlier `TMP_BaseShaderGUI.cs`
copied TMP's base GUI into `Assembly-CSharp-Editor` and broke whenever the host project's
TMP internals differed; the `.asmref` replaced that by compiling straight into TMP's editor
assembly. Keep it that way — don't re-copy TMP editor source in here.

## Working in here

- **Flag first.** Before adding a `using` for any third-party package, add a
  `DESSENTIALS_*` flag around it. The library must compile in a bare project.
- **No cross-dependencies.** Helpers are deliberately standalone. Don't make
  `RoundRobinQueue` depend on `EventBus` to save five lines.
- **Odin is optional.** Every `[Button]` / `[FoldoutGroup]` sits inside `#if ODIN_INSPECTOR`.
- **Editor code goes in an `Editor/` folder**, since there are no asmdefs to do it for you.
- Changes here affect every project that consumes the submodule — check the flags a feature
  is gated behind before changing its default branch.
