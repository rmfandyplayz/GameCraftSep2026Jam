# CLAUDE.md

Guidance for Claude Code when working in this repository.

**Keep this file current.** When a session changes something this file describes — a convention, a tool, a framework, a gotcha — update the relevant section in the same session, and add a new entry for anything non-obvious that was learned the hard way. The point of every entry below is to stop the next session rediscovering it. Prune entries that stop being true rather than letting them rot.

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
11. **A tween can supply its own easing function**, and that is the seam for anything that wants to reshape time rather than value. `DG.Tweening.EaseFunction` (`float (time, duration, overshootOrAmplitude, period)`), `SetEase(EaseFunction)` and `DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(Ease)` are all public in the installed DLL — so you can wrap a preset ease and hand DOTween a modified version of it. Verified working. There is **no** native frame rate, playback-rate or step/quantise setting; `Ease.Flash` is a flash, not a quantiser.

Project DOTween settings: safe mode **on**, tween recycling **off**, default ease `OutQuad`, default autoKill **on**.

## UI animation framework

`Assets/Scripts/UI/Utility/DOTweenAnimationPlayer/` holds a data-driven DOTween animation system — `UIAnimationPlayer` plays Inspector-authored named animations (`Play("Show")`). It is deliberately generic: it knows nothing about menus, HUDs, or transitions.

**Read that folder's `README.md` before changing it.** Do not add game-specific logic there. `Assets/Scripts/UI/Utility/` is a home for several small self-contained utilities, one folder each — do not flatten them back out.

**Reversing is an authoring operation, not a playback mode.** The `Mirror Animation` / `Duplicate as Mirrored` right-click commands rewrite the authored data once so you get a real second animation to tune — values, easings, `SetActive` and staggered delays all invert; punch/shake and `PlaySound` pass through. Nothing at runtime knows about mirroring, which is why the whole thing lives in `Editor/UIAnimationMirror.cs` and the runtime has no `PlayReverse`. **Do not add one back** without asking: a live reverse mode and an authored mirror are two answers to the same question, and having both was tried and cut.

**`SizeDelta` / `OffsetMin` / `OffsetMax` steps exist alongside `AnchoredPosition`, and all four write the same rect.** Unity stores one rect and derives every view from it, so animating two of them on one target makes them overwrite each other — measured on a 100×40 rect, writing `offsetMin` also changed `sizeDelta` and `anchoredPosition`. DOTween has a `DOSizeDelta` shortcut but **no** `DOOffsetMin`/`DOOffsetMax`, so those two use a generic `DOTween.To(getter, setter, …)`, which supports `From`, `SetRelative` and `SetOptions(snapping)` exactly like the shortcuts do. Any new Rect-driving step type must be added to `CaptureBaseline` too, or `To: Baseline` silently collapses the target to zero.

**`PlayAnimation` / `StopAnimation` are not redundant wrappers around `Play` / `Stop` — do not "clean them up".** Unity's UnityEvent dropdown lists only methods that return void and take at most one argument, which excludes `Play` (returns a `Sequence`) and `Stop` (an optional parameter still counts, so it reads as two). Verified against `UnityEditorInternal.UnityEventDrawer.CalculateMethodMap` in this Editor, not assumed. A void `Play(string)` cannot be an overload — C# does not overload on return type. `StopAll(bool)`, `ApplyFromState(string)` and `CaptureBaseline()` already qualified and need no wrapper.

**Stepped playback ("Play At Custom FPS") is implemented on the ease, not on the clock.** `UIAnimationSteppedEase` wraps a step's normal ease and quantises the *time* handed to it, so the tween still updates every frame but holds its value between frame boundaries. Consequences that are easy to get wrong: the end of a step is always sampled at its true end so it still lands exactly; quantisation uses the step's absolute position in the sequence as an offset so every step shares **one** grid (per-tween grids read as jitter); punch and shake are excluded because they drive their own oscillation rather than going through the ease; and instant steps keep their exact authored time. The frame rate is already a **per-step** parameter of `UIAnimationStep.BuildTween` — a per-step override would only change where `UIAnimationPlayer.BuildSequence` reads the number from, so do not re-plumb it.

UI sounds also live here: a `PlaySound` step plays an `AudioClip` at a point in an animation's timeline. With no AudioSource assigned it falls back to a shared 2D one (`UIAnimationAudio.Shared`, a `DontDestroyOnLoad` object created on first use). That is the **only** global in the folder — if you need UI audio routed through a mixer, call `UIAnimationAudio.SetShared` once rather than adding another.

Implementation notes that generalise beyond this folder:

- **Unity does not run C# field initialisers for elements added with `+` on a serialized `List<T>`.** A new element is zero-filled, so `= 1` / `= 0.25f` / `= "_Progress"` defaults in the class silently do not apply. The fix used here is a `FillUnsetDefaults()` on the serializable class, called from the MonoBehaviour's `OnValidate`, which only ever writes to fields still at their zero value so authored data is never clobbered. Any new inspector-authored data class in this project needs the same treatment.
- Validate shader property names against `Material.HasProperty` once at startup and warn, rather than letting `GetColor`/`SetFloat` error every frame.
- **Enums that appear on serialized fields are stored as integers — only ever append to them.** Inserting or reordering a value silently repoints every asset already authored against it. `UIAnimationStepType` carries a comment saying so.
- **`EditorApplication.contextualPropertyMenu` + `SerializedProperty.boxedValue`** is the cheap way to add copy/paste/transform commands to inspector-authored data (`Editor/UIAnimationContextMenu.cs`). Serialize with **`JsonUtility`, not `EditorJsonUtility`** — despite the name, it is the plain one that preserves `UnityEngine.Object` references, which is the whole point when the data holds scene targets. Measured: `JsonUtility.ToJson` writes `{"instanceID":-228892}`, `EditorJsonUtility.ToJson` writes `{"instanceID":0}` and the reference is silently gone. `EditorJsonUtility` serializes the way an asset file does, where a reference is a file ID that an in-memory scene object does not have. This was backwards here once and dropped every target slot on mirror and paste. `SerializedProperty.boxedValue` is itself a deep copy with live references, so it is a safe alternative to serializing at all. The property handed to the callback must be `.Copy()`d, since menu items run after it goes out of scope. **Register the event once per folder**: two handlers append to the same menu in whatever order their static constructors happened to run.
- **Unity derives an Inspector label from the field name**, so name the field the way the label should read — `PlayAtCustomFPS` renders as "Play At Custom FPS", and `FPS` stays "FPS". Renaming a serialized field to fix a label loses any data already authored against the old name, so get it right before anything is authored.
- **To hide a field behind a checkbox, write an attribute drawer, not a drawer for the containing class.** `UIAnimationShowIf` + its drawer is ~50 lines and leaves everything else alone; taking over `UIAnimation` itself would mean re-implementing its list UI and would sit in front of the reorder handles and the right-click copy/paste/mirror menu. A hidden field must return `-EditorGUIUtility.standardVerticalSpacing` from `GetPropertyHeight`, not `0`, or Unity's inter-property spacing leaves a visible gap.
- **`WithPrevious` steps are groups, not rows.** Anything that reorders a step list must keep each group's members together and contiguous, or it silently re-parents which steps are joined to which. `Editor/UIAnimationMirror.cs` is the reference implementation: it reverses the groups *and* reverses the members inside each one. That second part is required, not cosmetic polish — **in practice almost every authored animation is a single joined group**, so reversing only the groups looked like mirroring did nothing to the order at all. Reversing inside a group is safe because joined steps all offset from the same group start, so member order never affected timing; verified by sampling two orderings of the same group and getting identical output.
- **A step's `Delay` is applied in exactly one place: the insert position `UIAnimationPlayer.BuildSequence` computes.** `BuildTween` must never call `SetDelay` — DOTween adds a tween's own delay *on top of* its sequence position, so doing both silently doubles every delay. This regressed once already.

## Verifying changes

A Unity Editor is usually running with the Pipeline package connected. Compile-check for real rather than guessing:

```bash
unity status
unity cmd recompile
unity cmd recompile_status
unity cmd console -- --level error --tail 50
```

`unity list` shows all ~150 available commands. It takes no `--query`; the filter lives on the other subcommand — `unity cmd --query <term> --detail full` also prints each command's parameters.

**`eval` / `eval_file` code is compiled as a method body, not a file.** `using` directives are a syntax error there, so fully-qualify everything (`System.Text.StringBuilder`, `UnityEngine.Mathf`, `DG.Tweening.Ease`). `return` the value you want printed. To inspect editor GUI without dirtying a scene, build the object with `HideFlags.HideAndDontSave` and `DestroyImmediate` it in a `finally`.

**`recompile_status` is the authoritative result, not `console`.** `clear_console` returns `{"cleared":true}` but the Pipeline capture buffer keeps replaying old entries, so `console` will happily show you errors from a previous session — check their timestamps and file paths before believing them. A clean build is `{"status":"completed","failed":false,"errors":[]}`.

Treat the CLI as an **inspection and verification** tool. Do not use it (or `eval`) to modify gameplay systems, scenes, prefabs, materials, shaders, project settings, or assets, unless explicitly asked. Keep `eval` read-only.

## Known repo issue

`Assets/Scripts.meta` contains **unresolved git merge conflict markers** (`<<<<<<< HEAD` / `=======` / `>>>>>>>`) with two competing GUIDs. Unity recovers by string-matching and logs a warning on every refresh. It needs one of the two GUIDs picked and the markers deleted — but ask before touching it, since changing the surviving GUID would break references to the `Assets/Scripts` folder.
