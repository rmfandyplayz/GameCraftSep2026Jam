# UI Animation Utility

Data-driven DOTween animations for UGUI, authored in the Inspector instead of in code.

You define **named** animations (`Show`, `Hide`, `Hover`, `Attention`, `TransitionOut`…) on a component and play them by name. The framework attaches no meaning to any name — it just builds and plays the steps you authored.

---

## Components

| Component | Add it to | Why |
|---|---|---|
| **UI Animation Player** | any UI GameObject | Holds the named animations. This is the one you need. |
| **UI Material Instance** | an Image / RawImage / TMP text | Only needed for shader property animation. Gives the element its own material so tweens can never write to the shared project asset. |

---

## Creating an animation

1. **Add Component → UI Animation Player**.
2. `+` on **Animations**, set **Name** to `Show`.
3. `+` on that animation's **Steps**, pick a **Type**. The inspector collapses to only the fields that type uses.

Each step's collapsed header reads like a timeline line: `then   Scale   0.25s OutBack`.

### Step types

`AnchoredPosition` · `LocalPosition` · `Scale` · `Rotation` · `CanvasGroupAlpha` · `GraphicColor` · `GraphicAlpha` · `MaterialFloat` · `MaterialColor` · `PunchScale` · `PunchAnchoredPosition` · `ShakeAnchoredPosition` · `SetActive`

---

## Targets

Each step shows exactly one target slot, chosen by its type.

**Leave it empty to target the GameObject the player is on.** Drag in a child or sibling to target something else. That's the whole system — no reflection, no name lookups.

- `GraphicColor` / `GraphicAlpha` take a **Graphic**, which covers `Image`, `RawImage`, legacy `Text` **and TextMeshProUGUI**. There is no separate TMP step type.
- `AnchoredPosition` and `PunchAnchoredPosition` show an X/Y field — Z is not used.
- If a target is missing at play time the step is skipped with a console warning naming the animation and step index. It won't throw.

---

## Sequential vs parallel

Every step has a **Start** field:

- `AfterPrevious` → `Sequence.Append` — runs after everything before it.
- `WithPrevious` → `Sequence.Join` — runs alongside the previous step.

Per-step **Delay** works with both. Read the step list top to bottom and that's your timeline.

---

## Easing

Each step uses one of two easing sources:

- **Ease** — a DOTween preset. `Out*` eases decelerate into the end value and suit most UI.
- **Use Custom Curve** — tick it and the Ease dropdown is replaced by an `AnimationCurve` field.

For the curve, time runs 0→1 left to right, and value `0` = the FROM value, `1` = the TO value. Going above 1 or below 0 overshoots, which is how you build a bounce or an anticipation dip.

Unity's curve editor has a **preset bar along the bottom** — click a swatch to apply a shape, or use the arrow at the right of the bar to save your own. That's a native Unity feature, so curve shapes are reusable without any extra asset.

Punch and shake steps ignore both — DOTween drives their oscillation internally, so there's no ease field on them.

---

## FROM / TO

Each endpoint has a **mode**:

| Mode | Meaning |
|---|---|
| `Absolute` | Use the value exactly as typed. |
| `Baseline` | The element's resting value, captured at `Awake`, **plus** the typed value as an offset. |
| `Current` | Whatever the value is when the tween starts, plus the typed value (DOTween relative). Only available on **To**, and only when **Use From** is off. |

**Use `To: Baseline` for anything that should land on its authored resting state.** Ten `Show`s in a row all land on exactly the same value, and if you later change the resting scale/position in the scene the animation follows automatically. This is what stops repeated Show/Hide from drifting.

**Use From** (unticked by default) enables the FROM endpoint. **Apply From Values Immediately** (on by default, per animation) snaps every FROM value the moment the animation starts rather than when each step begins — this is what prevents an element flashing at full opacity through a delayed step before jumping to 0.

Punch and shake steps have no FROM/TO — they show `Punch` / `Strength` instead, and ignore Ease (they carry their own).

---

## Playing from code

```csharp
[SerializeField] private UIAnimationPlayer anim;

anim.Play("Show");
anim.Play("Hide", () => gameObject.SetActive(false));   // onComplete callback

// Play returns the live Sequence, so coroutines work:
yield return anim.Play("Show").WaitForCompletion();
```

Full API:

```csharp
Sequence Play(string name);
Sequence Play(string name, Action onComplete);

void Stop(string name, bool complete = false);
void StopAll(bool complete = false);

bool IsPlaying(string name);
bool IsAnyPlaying { get; }
bool Has(string name);

void ApplyFromState(string name);   // snap to an animation's FROM values without playing
void CaptureBaseline();             // re-capture resting values after moving things at runtime
```

There's also a **UnityEvent `On Complete`** per animation if you'd rather wire it in the Inspector.

`ApplyFromState` is the clean way to start hidden without authoring a separate state:

```csharp
private void Awake() => anim.ApplyFromState("Show");
```

In play mode the inspector shows **Play / From / Stop** buttons per animation so you can tune timing without a test script.

---

## Conflict and lifecycle behaviour

**Read this bit.**

- `Play(name)` kills that animation's own running sequence, and — because **Interrupt Others** is on by default — every *other* animation on the same player too. So `Play("Show"); Play("Hide");` leaves only `Hide` running. Untick **Interrupt Others** for something that should layer on top, like a looping pulse.
- **An interrupted animation never fires its callback.** Neither the `Action` nor the UnityEvent. If the callback fired, the animation genuinely finished.
- **Disabling the GameObject kills running animations** (`Kill On Disable`, on by default) — loops stop and callbacks do *not* fire. The next `Play` re-snaps its FROM values, so nothing ends up visually corrupted. Untick it to let animations run through a disable.
- Sequences are linked to the GameObject with `KillOnDestroy` and also killed in `OnDestroy`, so destroying objects or changing scenes leaves no orphaned tweens.
- **Use Unscaled Time** is on by default, so UI still animates while `Time.timeScale == 0`. Leave it on for pause menus.
- Baselines are captured once at `Awake`, before anything animates. If you move an element deliberately at runtime and want `Baseline` endpoints to follow, call `CaptureBaseline()` — but not mid-animation.

---

## Material / shader properties

Add **UI Material Instance** to the element. It clones the material at `Awake` and assigns the clone, so the shared project asset is never written to. `MaterialFloat` / `MaterialColor` steps target *that component* — there's no inspector slot that could point a tween at a shared asset by accident.

Set **Shader Property** to the property name (`_Progress`). It's resolved with `Shader.PropertyToID`.

Two real constraints:

- **TextMeshPro** ignores `Graphic.material` entirely, so the component uses TMP's own `fontMaterial` instead (and doesn't destroy it — TMP owns it). This works, but note you're animating TMP's material instance.
- **A stencil `Mask` breaks this.** UGUI takes a one-time cached copy of your material for stencil rendering and never re-syncs it, so animated properties won't reach the screen. Use a **RectMask2D** instead, or untick **Maskable** on the Graphic. The component logs a warning if it detects this.

Fullscreen transition overlays aren't inside masks, so the common case is unaffected.

---

## Example — Fade + Scale "Show"

Panel with a `CanvasGroup`, laid out in the scene at its final resting state. Every field below is shown exactly as the Inspector lists it, in order.

```
UI Animation Player
  Use Unscaled Time                ✔
  Kill On Disable                  ✔
  Animations                       1
    ▼ Show
        Name                       Show
        ▼ Steps                    2
            ▼ then  Canvas Group Alpha  0.25s  Out Quad
                Type               Canvas Group Alpha
                Start              After Previous
                Canvas Group       None            ← empty = this GameObject
                Duration           0.25
                Delay              0
                Use Custom Curve   ☐
                Ease               Out Quad
                Use From           ✔
                From   [Absolute]  0
                To     [Absolute]  1

            ▼ with  Scale  0.25s  Out Back
                Type               Scale
                Start              With Previous   ← parallel with the fade above
                Rect Transform     None
                Duration           0.25
                Delay              0
                Use Custom Curve   ☐
                Ease               Out Back
                Use From           ✔
                From   [Absolute]  X 0.8  Y 0.8  Z 0.8
                To     [Baseline]  X 0    Y 0    Z 0    ← lands on the authored scale
        Loops                      1
        Loop Type                  Restart
        Apply From Values Immediately  ✔
        Interrupt Others           ✔
        On Complete                (UnityEvent)
```

```csharp
private void Awake()    => anim.ApplyFromState("Show");
private void OnEnable() => anim.Play("Show");
public  void Close()    => anim.Play("Hide", () => gameObject.SetActive(false));
```

The `To [Baseline] (0,0,0)` on the scale step is the important part — `Baseline` means "the resting value captured at Awake, plus this offset", so a zero offset lands exactly on whatever scale you laid out in the scene.

---

## Example — fullscreen shader `_Progress`

Fullscreen `Image` → assign your transition material → **Add Component → UI Material Instance**.

```
UI Animation Player
  Use Unscaled Time                ✔
  Kill On Disable                  ✔
  Animations                       1
    ▼ TransitionOut
        Name                       TransitionOut
        ▼ Steps                    1
            ▼ then  Material Float  0.8s  In Out Quad
                Type               Material Float
                Start              After Previous
                Material Inst.     None            ← empty = this GameObject
                Shader Property    _Progress
                Duration           0.8
                Delay              0
                Use Custom Curve   ☐
                Ease               In Out Quad
                Use From           ✔
                From   [Absolute]  0
                To     [Absolute]  1
        Loops                      1
        Loop Type                  Restart
        Apply From Values Immediately  ✔
        Interrupt Others           ✔
        On Complete                (UnityEvent)
```

```csharp
transition.Play("TransitionOut", () => {
    // your scene loading here
});
```

**Shader Property must not be blank** and must exist on the material's shader. If it's missing or misspelled you get one clear warning at startup naming the shader and the property, and the step is skipped rather than spamming errors every frame.

---

## Gotchas

**Unity's `+` button does not run C# field initialisers.** A freshly added animation or step arrives zero-filled — `Loops 0`, `Duration 0`, `Ease Unset`, `Shader Property ""` — not with the defaults declared in code. The player's `OnValidate` patches this: it fills in any field still sitting at its zero value, and never touches a field you've already set. Two consequences worth knowing:

- **`Loops` means play count.** `1` = play once (normal), `2` = play twice, `-1` = loop forever. `0` is meaningless and is treated as `1`.
- If you genuinely want a `Duration` of `0`, it'll be bumped to `0.25`. Use `0.01` for an effectively instant tween, or a `SetActive` step.

Tip: once one step exists, `+` **duplicates the last step** rather than creating a blank one, which is usually what you want anyway.

**Every field has a tooltip.** Hover any label in a step for an explanation of what it does.

---

## Reuse

There are no ScriptableObject presets — steps hold scene references, so an asset-based preset would need a whole target-binding layer. Instead:

- Leave targets empty (= self) and an animation is fully portable.
- **Copy Component / Paste Component Values** to move a configured player to another element.
- Prefab variants for anything genuinely shared.
