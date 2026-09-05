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
using UnityEngine.Events;

/// <summary>
/// A named animation: an ordered list of steps plus playback options.
/// The framework attaches no meaning to the name - "Show" and "Wobble" are treated identically.
/// </summary>
[Serializable]
public class UIAnimation
{
    [Tooltip("The name you pass to Play(). Must be unique on this player.")]
    public string Name = "New Animation";

    [Tooltip("Runs top to bottom. Each step is appended after, or joined alongside, the one above it.")]
    public List<UIAnimationStep> Steps = new List<UIAnimationStep>();

    [Tooltip("How many times the whole animation plays. 1 = play once (the normal case). " +
             "2 = play twice. -1 = loop forever until stopped. 0 is not valid and is treated as 1.")]
    public int Loops = 1;

    [Tooltip("Restart = jump back to the start each loop. Yoyo = play forwards then backwards. " +
             "Only has an effect when Loops is not 1.")]
    public LoopType LoopType = LoopType.Restart;

    [Tooltip("Snap every FROM value to its target as soon as the animation starts, rather than " +
             "when each individual step begins. Prevents a visible flash on delayed steps.")]
    public bool ApplyFromValuesImmediately = true;

    [Tooltip("Kill every other animation on this player before starting. Turn off for " +
             "layered animations such as a looping pulse or a hover tint.")]
    public bool InterruptOthers = true;

    [Tooltip("Fires when the animation finishes naturally. Does NOT fire if it is interrupted, " +
             "stopped, or killed because the GameObject was disabled.")]
    public UnityEvent OnComplete;

    /// <summary>Effective loop count. Guards the meaningless 0 that a zero-initialised list element produces.</summary>
    public int EffectiveLoops
    {
        get { return Loops == 0 ? 1 : Loops; }
    }

    // Runtime state. One live Sequence per animation, owned here.
    [NonSerialized] public Sequence RuntimeSequence;
    [NonSerialized] public Action RuntimeCallback;

    public bool IsPlaying
    {
        get { return RuntimeSequence != null && RuntimeSequence.IsActive() && RuntimeSequence.IsPlaying(); }
    }

    public bool HasLiveSequence
    {
        get { return RuntimeSequence != null && RuntimeSequence.IsActive(); }
    }

    /// <summary>
    /// Fills in fields still at their zero value. Unity does not run C# field initialisers when
    /// you press + on a serialized list, so a fresh animation arrives with Loops 0 and no name.
    /// Only ever fills blanks. Editor-only, driven from UIAnimationPlayer.OnValidate.
    /// </summary>
    public void FillUnsetDefaults()
    {
        if (Loops == 0) Loops = 1;
        if (string.IsNullOrEmpty(Name)) Name = "New Animation";

        for (int i = 0; i < Steps.Count; i++)
        {
            if (Steps[i] != null) Steps[i].FillUnsetDefaults();
        }
    }
}
