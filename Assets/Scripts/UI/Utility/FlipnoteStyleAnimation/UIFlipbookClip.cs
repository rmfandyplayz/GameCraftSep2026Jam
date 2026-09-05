// -----------------------------------------------------------------------------
// Flipnote Style Animation Utility
//
// AI-GENERATED. Authored by Claude (Anthropic) via Claude Code, September 2026,
// to a written design brief by the project author. Not hand-written by the
// Twindrill Goose team. See the README.md beside this file for usage.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One hand-drawn flipbook: an ordered list of sprites plus timing.
///
/// This is plain serialized data with no runtime state of its own, so the same clip
/// asset-in-a-component can be handed to <see cref="UISpriteFlipbook.Play(UIFlipbookClip)"/>
/// as often as you like. Playback position lives on the player, not here.
/// </summary>
[Serializable]
public class UIFlipbookClip
{
    /// <summary>Frames per second used when a frame has no explicit duration.</summary>
    public const float DefaultFPS = 12f;

    [Tooltip("Frames in playback order, one sprite per drawing.")]
    public List<Sprite> Frames = new List<Sprite>();

    [Tooltip("Frames per second. 6-12 reads as hand-drawn; 24 is smooth.")]
    public float FPS = DefaultFPS;

    [Tooltip("Loop forever. Off = play once and hold on the last frame.")]
    public bool Loop = true;

    [Tooltip("OPTIONAL hand-timing. Hold time in seconds for each frame, matched to Frames by index. " +
             "Leave the whole list empty for plain constant FPS. Any entry left at 0 - and any frame " +
             "past the end of this list - falls back to the FPS time, so you only fill in the frames " +
             "you actually want to hold longer.")]
    public List<float> FrameDurations = new List<float>();

    // Set the first time defaults are filled. Without it, FillUnsetDefaults could not tell
    // "Loop was never authored" (which should become true) from "the user unticked Loop".
    [SerializeField, HideInInspector] private bool defaultsFilled;

    public int FrameCount
    {
        get { return Frames == null ? 0 : Frames.Count; }
    }

    public bool HasFrames
    {
        get { return FrameCount > 0; }
    }

    /// <summary>How long frame <paramref name="index"/> stays on screen, in seconds. Always &gt; 0.</summary>
    public float DurationOf(int index)
    {
        if (FrameDurations != null && index >= 0 && index < FrameDurations.Count && FrameDurations[index] > 0f)
        {
            return FrameDurations[index];
        }

        return 1f / (FPS > 0f ? FPS : DefaultFPS);
    }

    /// <summary>
    /// Unity does not run C# field initialisers for elements added with "+" on a serialized list,
    /// so a new clip arrives zero-filled (FPS 0, Loop off). Call this from the owner's OnValidate.
    /// It only ever writes fields that are still unauthored, so it never clobbers real data.
    /// </summary>
    public void FillUnsetDefaults()
    {
        if (Frames == null)
        {
            Frames = new List<Sprite>();
        }

        if (FrameDurations == null)
        {
            FrameDurations = new List<float>();
        }

        if (FPS <= 0f)
        {
            FPS = DefaultFPS;
        }

        if (!defaultsFilled)
        {
            defaultsFilled = true;
            Loop = true;
        }
    }
}
