// -----------------------------------------------------------------------------
// UI Animation Utility
//
// AI-GENERATED. Authored by Claude (Anthropic) via Claude Code, September 2026,
// to a written design brief by the project author. Not hand-written by the
// Twindrill Goose team. See Assets/Scripts/UI/Utility/README.md for usage.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Plays named, inspector-authored DOTween animations on UI elements.
///
/// The framework has no idea what any animation name means - it just builds and plays
/// whatever steps you authored. Drive it from your own scripts:
///
///     player.Play("Show");
///     player.Play("Hide", () => gameObject.SetActive(false));
///
/// See README.md in this folder for the authoring workflow.
/// </summary>
[DisallowMultipleComponent]
public class UIAnimationPlayer : MonoBehaviour
{
    [Tooltip("Run animations on unscaled time so UI still animates while Time.timeScale is 0 (pause menus).")]
    [SerializeField] private bool UseUnscaledTime = true;

    [Tooltip("Kill running animations when this GameObject is disabled. Completion callbacks do not fire.")]
    [SerializeField] private bool KillOnDisable = true;

    [SerializeField] private List<UIAnimation> Animations = new List<UIAnimation>();

    private readonly Dictionary<string, int> lookup = new Dictionary<string, int>();
    private bool initialized;

    /// <summary>True while any animation on this player is running.</summary>
    public bool IsAnyPlaying
    {
        get
        {
            for (int i = 0; i < Animations.Count; i++)
            {
                if (Animations[i].IsPlaying) return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        if (KillOnDisable) StopAll();
    }

    private void OnDestroy()
    {
        StopAll();
    }

    // ---------------------------------------------------------------- public API

    /// <summary>Plays a named animation. Returns the live Sequence, or null if the name is unknown.</summary>
    public Sequence Play(string animationName)
    {
        return Play(animationName, null);
    }

    /// <summary>
    /// Plays a named animation and invokes onComplete when it finishes naturally.
    /// The callback does NOT fire if the animation is interrupted, stopped, or killed on disable.
    /// </summary>
    public Sequence Play(string animationName, Action onComplete)
    {
        Initialize();

        UIAnimation animation = Find(animationName);
        if (animation == null) return null;

        Kill(animation, false);

        if (animation.InterruptOthers)
        {
            for (int i = 0; i < Animations.Count; i++)
            {
                if (Animations[i] != animation) Kill(Animations[i], false);
            }
        }

        Sequence sequence = BuildSequence(animation);
        if (sequence == null)
        {
            // Nothing to play. Fire the callbacks anyway so caller logic never hangs.
            if (onComplete != null) onComplete.Invoke();
            if (animation.OnComplete != null) animation.OnComplete.Invoke();
            return null;
        }

        animation.RuntimeSequence = sequence;
        animation.RuntimeCallback = onComplete;

        UIAnimation captured = animation;
        sequence.OnComplete(() =>
        {
            captured.RuntimeSequence = null;
            Action callback = captured.RuntimeCallback;
            captured.RuntimeCallback = null;

            if (callback != null) callback.Invoke();
            if (captured.OnComplete != null) captured.OnComplete.Invoke();
        });

        sequence.SetUpdate(UseUnscaledTime);

        // Must be applied to the outer Sequence - SetLink is a no-op on tweens inside one.
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (animation.Loops != 1) sequence.SetLoops(animation.Loops, animation.LoopType);

        sequence.Play();
        return sequence;
    }

    /// <summary>Stops one animation. complete=true jumps to the end state and fires its callbacks.</summary>
    public void Stop(string animationName, bool complete = false)
    {
        UIAnimation animation = Find(animationName);
        if (animation != null) Kill(animation, complete);
    }

    /// <summary>Stops every animation on this player.</summary>
    public void StopAll(bool complete = false)
    {
        for (int i = 0; i < Animations.Count; i++)
        {
            Kill(Animations[i], complete);
        }
    }

    public bool IsPlaying(string animationName)
    {
        UIAnimation animation = FindQuiet(animationName);
        return animation != null && animation.IsPlaying;
    }

    public bool Has(string animationName)
    {
        Initialize();
        return lookup.ContainsKey(animationName);
    }

    /// <summary>
    /// Snaps every FROM value of an animation to its target without playing anything.
    /// Use this to put an element into its hidden start state, e.g. ApplyFromState("Show") in Awake.
    /// </summary>
    public void ApplyFromState(string animationName)
    {
        Initialize();

        UIAnimation animation = Find(animationName);
        if (animation == null) return;

        for (int i = 0; i < animation.Steps.Count; i++)
        {
            animation.Steps[i].ApplyFromValue();
        }
    }

    /// <summary>
    /// Re-reads the resting values that Baseline endpoints are relative to.
    /// Call after deliberately moving an element at runtime. Do not call mid-animation.
    /// </summary>
    public void CaptureBaseline()
    {
        Initialize();

        for (int i = 0; i < Animations.Count; i++)
        {
            List<UIAnimationStep> steps = Animations[i].Steps;
            for (int s = 0; s < steps.Count; s++)
            {
                steps[s].CaptureBaseline();
            }
        }
    }

    // ---------------------------------------------------------------- internals

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        lookup.Clear();

        for (int i = 0; i < Animations.Count; i++)
        {
            UIAnimation animation = Animations[i];

            if (string.IsNullOrEmpty(animation.Name))
            {
                Debug.LogWarning("UIAnimationPlayer on '" + name + "': animation " + i + " has no name.", this);
            }
            else if (lookup.ContainsKey(animation.Name))
            {
                Debug.LogWarning(
                    "UIAnimationPlayer on '" + name + "': duplicate animation name '" + animation.Name +
                    "'. The first one wins.", this);
            }
            else
            {
                lookup.Add(animation.Name, i);
            }

            List<UIAnimationStep> steps = animation.Steps;
            for (int s = 0; s < steps.Count; s++)
            {
                steps[s].Resolve(gameObject);
            }
        }

        // Baselines are captured after every target is resolved and before anything animates,
        // so all steps sharing a target agree on the same resting value.
        for (int i = 0; i < Animations.Count; i++)
        {
            List<UIAnimationStep> steps = Animations[i].Steps;
            for (int s = 0; s < steps.Count; s++)
            {
                steps[s].CaptureBaseline();
            }
        }
    }

    private Sequence BuildSequence(UIAnimation animation)
    {
        List<UIAnimationStep> steps = animation.Steps;
        if (steps.Count == 0) return null;

        Sequence sequence = DOTween.Sequence();
        sequence.SetAutoKill(true);

        // Track layout manually rather than reading Sequence.Duration() mid-construction.
        float sequenceEnd = 0f;
        float lastStepStart = 0f;
        bool anyContent = false;

        for (int i = 0; i < steps.Count; i++)
        {
            UIAnimationStep step = steps[i];
            bool append = i == 0 || step.Start == UIAnimationStartMode.AfterPrevious;
            float stepStart = append ? sequenceEnd : lastStepStart;

            if (step.Type == UIAnimationStepType.SetActive)
            {
                UIAnimationStep captured = step;
                sequence.InsertCallback(stepStart + step.Delay, () => captured.ApplyActiveValue());
                anyContent = true;
            }
            else
            {
                string context = "UIAnimationPlayer on '" + name + "' animation '" + animation.Name + "' step " + i;
                Tween tween = step.BuildTween(animation.ApplyFromValuesImmediately, context);
                if (tween == null) continue;

                if (append) sequence.Append(tween);
                else sequence.Join(tween);

                anyContent = true;
            }

            if (append) lastStepStart = stepStart;
            sequenceEnd = Mathf.Max(sequenceEnd, stepStart + step.TotalDuration);
        }

        if (!anyContent)
        {
            sequence.Kill();
            return null;
        }

        return sequence;
    }

    private void Kill(UIAnimation animation, bool complete)
    {
        if (animation.RuntimeSequence == null) return;

        Sequence sequence = animation.RuntimeSequence;

        if (!complete)
        {
            // Drop the callbacks first so an interrupted animation never reports completion.
            animation.RuntimeSequence = null;
            animation.RuntimeCallback = null;
        }

        if (sequence.IsActive()) sequence.Kill(complete);

        animation.RuntimeSequence = null;
        animation.RuntimeCallback = null;
    }

    private UIAnimation Find(string animationName)
    {
        UIAnimation animation = FindQuiet(animationName);
        if (animation == null)
        {
            Debug.LogWarning("UIAnimationPlayer on '" + name + "': no animation named '" + animationName + "'.", this);
        }

        return animation;
    }

    private UIAnimation FindQuiet(string animationName)
    {
        Initialize();

        int index;
        if (animationName == null || !lookup.TryGetValue(animationName, out index)) return null;
        return Animations[index];
    }

#if UNITY_EDITOR
    /// <summary>Editor-only accessor for the inspector play buttons.</summary>
    public List<UIAnimation> EditorAnimations
    {
        get { return Animations; }
    }
#endif
}
