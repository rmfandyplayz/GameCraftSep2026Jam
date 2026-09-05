# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

**AntFlood** — a 48-hour game jam project (GameCraft, Sept 2026). Company `TwindrillGoose`.

- **Unity 6000.3.9f1** (Unity 6.3), **URP 17.3.0** (active asset `Assets/Settings/PC_RPAsset.asset`)
- **UGUI + TextMeshPro** for UI. No UI Toolkit — there are zero `.uxml`/`.uss` files, and DOTween's UI Toolkit module is disabled.
- **New Input System only** (`activeInputHandler: 1`). `UnityEngine.Input` will throw — use `UnityEngine.InputSystem`.
- **DOTween Pro 1.0.430** at `Assets/Plugins/Demigiant/`.
- **`com.unity.pipeline`** is installed, so the `unity` CLI can drive the running Editor. See *Verifying changes*.

Jam project: prefer small, obvious, working code over architecture. No DI, service locators, message buses, or deep inheritance.

## Layout

```
Assets/Scripts/Game/          gameplay (AntManager, IAntInteractable, ICarriableObject)
Assets/Scripts/UI/Utility/    small self-contained UI utilities, one folder each
  DOTweenAnimationPlayer/     the UI animation framework + its README.md
Assets/Scenes/                SampleScene.unity
Assets/Scenes/TestScenes/     UITest.unity, EliTest.unity
Assets/Plugins/Demigiant/     DOTween Pro (do not edit)
Assets/Settings/              URP pipeline assets
```

## Conventions

Derived from `Assets/Scripts/Game/AntManager.cs`:

- **No namespaces.** Root namespace is empty; everything is global.
- 4 spaces, Allman braces (opening brace on its own line).
- `[SerializeField] private` with **PascalCase** field names (`MoveSpeed`, `CamTargetTransform`).
- **camelCase, no underscore prefix** for private non-serialized fields (`playerCamera`, `moveInput`).
- Interfaces prefixed `I`. `var` used freely for locals.
- No `#nullable`. C# 9, .NET Standard 2.1.

## Assembly definitions — do not add any

There are **zero `.asmdef` files under `Assets/`**, and that is deliberate.

DOTween's `Modules/*.cs` live under `Assets/Plugins`, so they compile into the predefined assembly `Assembly-CSharp-firstpass`. An asmdef **cannot reference a predefined assembly**, so adding one would silently break `DOAnchorPos`, `DOFade`, `DOColor`, `DOPunchAnchorPos`, `DOShakeAnchorPos` and every DOTween Pro TMP shortcut. Everything in `Assets/Scripts/**` currently lives in `Assembly-CSharp` and sees DOTween with no setup.

## DOTween gotchas verified in this project

These bite silently — no error, no warning:

1. **`SetLink` is a no-op on a tween that is inside a Sequence.** Apply it to the outer `Sequence` only.
2. **Configure a tween fully *before* `Append`/`Join`.** `Sequence.DoInsert` sets `creationLocked`, after which `From`, `SetEase`, `SetRelative` etc. silently do nothing.
3. **`.From(value)` defaults to `setImmediately: true`**, which writes the target during sequence *construction*, not at t=0 of that tween. Pass `false` to defer.
4. `Material` shortcuts (`DOFloat`, `DOColor` with a property ID) exist in core `DOTween.dll` and return `TweenerCore`, so `.From()` works on them.
5. `TMP_Text` derives from `Graphic` and overrides `color`, so `DOColor`/`DOFade` on a `Graphic` reference drives TMP correctly — no TMP-specific code needed for colour/alpha.
6. **`TextMeshProUGUI` ignores `Graphic.material`** (it overrides `materialForRendering` to use `m_sharedMaterial`). Use `TMP_Text.fontMaterial`, and don't destroy it — TMP owns it.
7. **A stencil `Mask` defeats material instancing.** `StencilMaterial.Add` takes a one-time cached copy and never re-syncs, so property writes don't reach the screen. `RectMask2D` is unaffected.
8. `MaterialPropertyBlock` does **not** work with UGUI — `CanvasRenderer` ignores it.
9. `DOTween.Init` is not needed (auto-inits). `SetUpdate(true)` works on `Sequence` and is required for UI that animates while `Time.timeScale == 0`.
10. **A tween's `SetDelay` is added *on top of* its position in a Sequence, not instead of it.** Measured against the loaded DLL: `Append(t)` and `Join(t)` place `t` at `groupStart + t.delay`; `Insert(pos, t)` places it at `pos + t.delay`. So code that positions tweens explicitly must not also call `SetDelay`. `Join` offsets from the *group* start, not from the previous step's own start.

Project DOTween settings: safe mode **on**, tween recycling **off**, default ease `OutQuad`, default autoKill **on**.

## UI animation framework

`Assets/Scripts/UI/Utility/DOTweenAnimationPlayer/` holds a data-driven DOTween animation system — `UIAnimationPlayer` plays Inspector-authored named animations (`Play("Show")`). It is deliberately generic: it knows nothing about menus, HUDs, or transitions.

**Read that folder's `README.md` before changing it.** Do not add game-specific logic there. `Assets/Scripts/UI/Utility/` is a home for several small self-contained utilities, one folder each — do not flatten them back out.

**Reversing is an authoring operation, not a playback mode.** The `Mirror Animation` / `Duplicate as Mirrored` right-click commands rewrite the authored data once so you get a real second animation to tune — values, easings, `SetActive` and staggered delays all invert; punch/shake and `PlaySound` pass through. Nothing at runtime knows about mirroring, which is why the whole thing lives in `Editor/UIAnimationMirror.cs` and the runtime has no `PlayReverse`. **Do not add one back** without asking: a live reverse mode and an authored mirror are two answers to the same question, and having both was tried and cut.

UI sounds also live here: a `PlaySound` step plays an `AudioClip` at a point in an animation's timeline. With no AudioSource assigned it falls back to a shared 2D one (`UIAnimationAudio.Shared`, a `DontDestroyOnLoad` object created on first use). That is the **only** global in the folder — if you need UI audio routed through a mixer, call `UIAnimationAudio.SetShared` once rather than adding another.

Implementation notes that generalise beyond this folder:

- **Unity does not run C# field initialisers for elements added with `+` on a serialized `List<T>`.** A new element is zero-filled, so `= 1` / `= 0.25f` / `= "_Progress"` defaults in the class silently do not apply. The fix used here is a `FillUnsetDefaults()` on the serializable class, called from the MonoBehaviour's `OnValidate`, which only ever writes to fields still at their zero value so authored data is never clobbered. Any new inspector-authored data class in this project needs the same treatment.
- Validate shader property names against `Material.HasProperty` once at startup and warn, rather than letting `GetColor`/`SetFloat` error every frame.
- **Enums that appear on serialized fields are stored as integers — only ever append to them.** Inserting or reordering a value silently repoints every asset already authored against it. `UIAnimationStepType` carries a comment saying so.
- **`EditorApplication.contextualPropertyMenu` + `SerializedProperty.boxedValue`** is the cheap way to add copy/paste/transform commands to inspector-authored data (`Editor/UIAnimationContextMenu.cs`). Serialize with `EditorJsonUtility`, not `JsonUtility` — only the editor one preserves `UnityEngine.Object` references, which is the whole point when the data holds scene targets. The property handed to the callback must be `.Copy()`d, since menu items run after it goes out of scope. **Register the event once per folder**: two handlers append to the same menu in whatever order their static constructors happened to run.
- **`WithPrevious` steps are groups, not rows.** Anything that reorders a step list must reverse the *groups* and keep each group's members together, or it silently re-parents which steps are joined to which. `Editor/UIAnimationMirror.cs` is the reference implementation.
- **A step's `Delay` is applied in exactly one place: the insert position `UIAnimationPlayer.BuildSequence` computes.** `BuildTween` must never call `SetDelay` — DOTween adds a tween's own delay *on top of* its sequence position, so doing both silently doubles every delay. This regressed once already.

## Verifying changes

A Unity Editor is usually running with the Pipeline package connected. Compile-check for real rather than guessing:

```bash
unity status
unity cmd recompile
unity cmd recompile_status
unity cmd console -- --level error --tail 50
```

`unity list` shows all ~150 available commands; `unity list --query <term>` filters them.

**`recompile_status` is the authoritative result, not `console`.** `clear_console` returns `{"cleared":true}` but the Pipeline capture buffer keeps replaying old entries, so `console` will happily show you errors from a previous session — check their timestamps and file paths before believing them. A clean build is `{"status":"completed","failed":false,"errors":[]}`.

Treat the CLI as an **inspection and verification** tool. Do not use it (or `eval`) to modify gameplay systems, scenes, prefabs, materials, shaders, project settings, or assets, unless explicitly asked. Keep `eval` read-only.

## Known repo issue

`Assets/Scripts.meta` contains **unresolved git merge conflict markers** (`<<<<<<< HEAD` / `=======` / `>>>>>>>`) with two competing GUIDs. Unity recovers by string-matching and logs a warning on every refresh. It needs one of the two GUIDs picked and the markers deleted — but ask before touching it, since changing the surviving GUID would break references to the `Assets/Scripts` folder.
