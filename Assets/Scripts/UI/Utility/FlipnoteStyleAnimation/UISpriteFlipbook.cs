// -----------------------------------------------------------------------------
// Flipnote Style Animation Utility
//
// AI-GENERATED. Authored by Claude (Anthropic) via Claude Code, September 2026,
// to a written design brief by the project author. Not hand-written by the
// Twindrill Goose team. See the README.md beside this file for usage.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cycles a UI Image through hand-drawn sprite frames. No Animator, no Animation Clips.
///
///     flipbook.Play();                // the clip authored on this component
///     flipbook.Play(someOtherClip);   // any clip, e.g. from UIFlipbookButton
///     flipbook.Stop();
///     flipbook.Restart();
///
/// Only sprite frames are this component's business. Let DOTween (or anything else)
/// animate the same object's position, scale and colour independently.
/// </summary>
[DisallowMultipleComponent]
public class UISpriteFlipbook : MonoBehaviour
{
    // Never step more than this many frames in a single Update. Stops a huge frame hitch,
    // or an absurd FPS value, from spinning the catch-up loop.
    private const int MaxCatchUpSteps = 64;

    [Tooltip("The Image whose sprite is swapped. Left empty, an Image on this GameObject is used.")]
    [SerializeField] private Image TargetImage;

    [Tooltip("The clip played by Play() with no arguments.")]
    [SerializeField] private UIFlipbookClip Clip = new UIFlipbookClip();

    [Tooltip("Start the clip above automatically. Turn this OFF when something else drives this " +
             "flipbook, such as a UIFlipbookButton.")]
    [SerializeField] private bool PlayOnEnable = true;

    [Tooltip("Advance on unscaled time so the drawing keeps flipping while Time.timeScale is 0 " +
             "(pause menus, hit-stop).")]
    [SerializeField] private bool UseUnscaledTime = true;

    /// <summary>
    /// Fires when a non-looping clip runs off its last frame. Never fires for looping clips,
    /// and never fires when playback is replaced by another Play() or cut short by Stop().
    /// </summary>
    public event Action Completed;

    private UIFlipbookClip current;
    private int frameIndex = -1;
    private float frameTimer;
    private bool playing;

    /// <summary>True while frames are advancing.</summary>
    public bool IsPlaying
    {
        get { return playing; }
    }

    /// <summary>The clip currently playing, or the last one played. Null until something plays.</summary>
    public UIFlipbookClip CurrentClip
    {
        get { return current; }
    }

    /// <summary>Index of the frame on screen, or -1 if nothing has been shown yet.</summary>
    public int CurrentFrame
    {
        get { return frameIndex; }
    }

    /// <summary>Play the clip authored on this component.</summary>
    public void Play()
    {
        Play(Clip, true);
    }

    /// <summary>Play a clip. If it is already the clip running, playback is left alone.</summary>
    public void Play(UIFlipbookClip clip)
    {
        Play(clip, false);
    }

    /// <summary>
    /// Play a clip, always from frame 0 when <paramref name="restartIfAlreadyPlaying"/> is true.
    /// A null or empty clip is the same as <see cref="Stop"/>.
    /// </summary>
    public void Play(UIFlipbookClip clip, bool restartIfAlreadyPlaying)
    {
        if (clip == null || !clip.HasFrames)
        {
            Stop();
            return;
        }

        if (!restartIfAlreadyPlaying && playing && ReferenceEquals(clip, current))
        {
            return;
        }

        // Full reset, so switching clips can never leave a stale timer or index behind.
        current = clip;
        frameTimer = 0f;
        frameIndex = -1;
        playing = true;

        ShowFrame(0);
    }

    /// <summary>Stop advancing and leave the current drawing on screen.</summary>
    public void Stop()
    {
        playing = false;
        frameTimer = 0f;
    }

    /// <summary>Restart the current clip - or the authored one, if nothing has played yet - from frame 0.</summary>
    public void Restart()
    {
        Play(current != null ? current : Clip, true);
    }

    private void Awake()
    {
        ResolveTarget();
    }

    private void OnEnable()
    {
        ResolveTarget();

        // Something was playing when we were switched off: restart it rather than resuming
        // mid-frame, so re-enabling is always predictable.
        if (playing && current != null)
        {
            Play(current, true);
            return;
        }

        // First enable only. After an explicit Stop(), current is set, so we stay stopped.
        if (PlayOnEnable && current == null)
        {
            Play(Clip, true);
        }
    }

    // OnDisable needs no work: Unity stops calling Update, and `playing` staying true is
    // exactly the flag OnEnable reads to decide whether to resume.

    private void Update()
    {
        if (!playing)
        {
            return;
        }

        if (current == null || !current.HasFrames || TargetImage == null)
        {
            // Clip emptied in the inspector, or the Image was destroyed out from under us.
            playing = false;
            return;
        }

        frameTimer += UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        var duration = current.DurationOf(frameIndex);
        var steps = 0;

        while (playing && frameTimer >= duration && steps++ < MaxCatchUpSteps)
        {
            frameTimer -= duration;
            Advance();

            if (!playing)
            {
                break;
            }

            duration = current.DurationOf(frameIndex);
        }
    }

    private void Advance()
    {
        var next = frameIndex + 1;

        if (next >= current.FrameCount)
        {
            if (!current.Loop)
            {
                // Hold the last frame. Clear state before the callback, which is free to Play() again.
                playing = false;
                frameTimer = 0f;

                var completed = Completed;
                if (completed != null)
                {
                    completed();
                }

                return;
            }

            next = 0;
        }

        ShowFrame(next);
    }

    private void ShowFrame(int index)
    {
        frameIndex = index;

        if (TargetImage == null || current == null || index < 0 || index >= current.FrameCount)
        {
            return;
        }

        var sprite = current.Frames[index];

        // Writing Image.sprite dirties the canvas, so only write on a real change.
        // Repeated nulls in a frame list are legitimate (a blank beat in the drawing).
        if (!ReferenceEquals(TargetImage.sprite, sprite))
        {
            TargetImage.sprite = sprite;
        }
    }

    private void ResolveTarget()
    {
        if (TargetImage == null)
        {
            TargetImage = GetComponent<Image>();
        }
    }

    private void Reset()
    {
        TargetImage = GetComponent<Image>();

        if (Clip != null)
        {
            Clip.FillUnsetDefaults();
        }
    }

    private void OnValidate()
    {
        if (Clip != null)
        {
            Clip.FillUnsetDefaults();
        }
    }
}
