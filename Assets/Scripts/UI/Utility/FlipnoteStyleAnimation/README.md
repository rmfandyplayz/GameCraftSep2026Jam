# Flipnote Style Animation

Sprite-frame flipbooks for UGUI `Image`s. No Animator Controllers, no Animation Clips,
no ScriptableObjects — everything is authored directly on the component.

Three files:

| File | What it is |
| --- | --- |
| `UIFlipbookClip.cs` | Serializable data: frames + FPS + loop + optional per-frame holds |
| `UISpriteFlipbook.cs` | Plays a clip onto an `Image` |
| `UIFlipbookButton.cs` | Picks the clip for a `Button`'s interaction state |

**This is completely separate from `DOTweenAnimationPlayer/`.** It only ever writes
`Image.sprite`. DOTween can move, scale, rotate and fade the same object at the same
time with no conflict — that is the intended split.

## A plain flipbook

1. Add `UISpriteFlipbook` to a GameObject that has an `Image`.
   `Target Image` fills itself in.
2. Drag your sprites into `Clip > Frames`, in order.
3. Set `FPS` (8–12 reads as hand-drawn) and `Loop`.

That's it — `Play On Enable` is on by default, so it runs as soon as the object is enabled.

From script:

```csharp
flipbook.Play();       // the clip authored on the component, from frame 0
flipbook.Play(clip);   // any clip; no-op if that clip is already running
flipbook.Play(clip, true);  // ...same, but always restart from frame 0
flipbook.Stop();       // stop advancing, hold the current drawing
flipbook.Restart();
flipbook.Completed += () => { ... };  // non-looping clips only
```

`Completed` fires only when a non-looping clip runs off its last frame. It does not fire
when playback is replaced by another `Play()`, cut short by `Stop()`, or interrupted by
the GameObject being disabled.

### Optional hand-timing

`FPS` is the easy path and stays the default. When a drawing needs to hang, fill in
`Frame Durations` — seconds, matched to `Frames` by index:

```
Frames:          [ pose_a, pose_b, pose_c ]
FPS:             12
Frame Durations: [ 0.5,    0,      0      ]
```

`pose_a` holds half a second; the two entries left at `0` fall back to the FPS time
(1/12 s). Entries past the end of the list fall back too, so a list shorter than
`Frames` is fine — you only fill in what you want to change. Leave the whole list
empty for plain constant FPS.

## A flipbook Button

1. Keep your normal `Button`. Set its **Transition to `None`** (or `Color Tint`).
   *Do not use `Sprite Swap`* — it writes `Image.sprite` itself and will fight the
   flipbook. `UIFlipbookButton` logs a warning if it sees it.
2. Add `UISpriteFlipbook` and turn **`Play On Enable` off** (the button drives it).
3. Add `UIFlipbookButton`. `Target` and `Flipbook` fill themselves in — `Flipbook` is
   looked up on this object first, then in children, so the visual can be a child Image.
4. Fill in the state clips:

```
Normal        Frames [normal_01 … 03]    FPS 8    Loop ✓
Highlighted   Frames [hover_01  … 03]    FPS 10   Loop ✓
Pressed       Frames [pressed_01, 02]    FPS 12   Loop ✓
Selected      (leave empty unless you want a focus look)
Disabled      Frames [disabled_01, 02]   FPS 6    Loop ✓
```

`onClick`, navigation and every other Button behaviour are untouched. `UIFlipbookButton`
implements the standard EventSystem handler interfaces and sits *beside* the Button —
the EventSystem delivers each event to every handler on the object, so both receive it.

Mouse hover/press works, and keyboard/controller works: `ISelectHandler` drives the
Selected state and `ISubmitHandler` flashes the Pressed clip for `Submit Press Duration`
(0.1 s), since a controller submit has no press-and-hold to read.

It takes a `Selectable`, not a `Button`, so it works on a Toggle or a Slider handle too.

## State priority

Exactly the order `UnityEngine.UI.Selectable` uses internally, so the flipbook and the
Button's own transition never disagree:

```
Disabled  >  Pressed  >  Selected  >  Highlighted  >  Normal
```

Note that **Selected outranks Highlighted** — a focused button that you also hover shows
Selected. That is Unity's behaviour, not something added here.

A state with no frames falls through:

| State | Falls back to |
| --- | --- |
| Disabled | Normal |
| Pressed | Highlighted → Normal |
| Selected | **Normal** |
| Highlighted | Normal |

Selected deliberately does *not* fall back to Highlighted. A button keeps EventSystem
selection after you click it, so borrowing the hover look while the mouse is somewhere
else reads as a stuck button. Leaving `Selected` empty therefore means "no focus look",
which is usually what you want on a mouse-only jam build.

Entering a state always restarts its clip from frame 0, and the flipbook holds exactly
one clip at a time — `Hover → Pressed → Hover` can never leave two loops running.

## Enabled / disabled

- Disabling the GameObject stops `Update`, so nothing ticks.
- Re-enabling **restarts** the clip from frame 0 rather than resuming mid-frame, so it is
  always predictable. `UIFlipbookButton` re-reads hover/press/selection on enable and
  force-applies the right state, because no exit event arrives while a component is off.
- After an explicit `Stop()`, re-enabling stays stopped — `Play On Enable` only fires the
  first time.
- If the `Image` is destroyed underneath a running flipbook, it stops instead of throwing.

## Paused / unscaled time

`Use Unscaled Time` is **on by default** on both components, so flipbooks keep running
while `Time.timeScale == 0`. That is the right default for pause menus — hand-drawn UI
that freezes when you pause looks broken.

Turn it off per component for a flipbook that is part of the gameplay layer and *should*
freeze with the world (an in-world sign, a diegetic HUD element that stops with hit-stop).

`UIFlipbookButton` has its own `Use Unscaled Time`, which only times the Submit press
flash. Set it to match its flipbook.

## Costs

- No per-frame allocations. No coroutines, no LINQ, no `params` arrays on the state path.
- `Image.sprite` is only written when the frame actually changes, so the canvas is not
  re-dirtied every tick.
- `UIFlipbookButton.Update` recomputes its state every frame (a handful of bool reads).
  It has to: `Selectable.interactable` can be set from code with no event to hook, so
  polling is the only way the Disabled state can work at all. The clip only switches when
  the resolved state actually changed.

## Gotcha inherited from the rest of the project

Unity does not run C# field initialisers for elements added with `+` on a serialized
`List<T>` — a new element arrives zero-filled, so `FPS = 12f` and `Loop = true` would
silently not apply. `UIFlipbookClip.FillUnsetDefaults()` fixes that up from the owner's
`OnValidate`, and only ever writes fields that are still unauthored. The `Loop` default
needs the hidden `defaultsFilled` flag specifically because `false` is both "never set"
and "the user unticked it".
