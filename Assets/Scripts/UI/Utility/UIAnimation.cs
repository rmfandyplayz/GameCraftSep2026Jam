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
    public string Name = "New Animation";
    public List<UIAnimationStep> Steps = new List<UIAnimationStep>();

    [Tooltip("1 = play once. -1 = loop forever.")]
    public int Loops = 1;

    public LoopType LoopType = LoopType.Restart;

    [Tooltip("Snap every FROM value to its target as soon as the animation starts, rather than " +
             "when each individual step begins. Prevents a visible flash on delayed steps.")]
    public bool ApplyFromValuesImmediately = true;

    [Tooltip("Kill every other animation on this player before starting. Turn off for " +
             "layered animations such as a looping pulse or a hover tint.")]
    public bool InterruptOthers = true;

    public UnityEvent OnComplete;

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
}
