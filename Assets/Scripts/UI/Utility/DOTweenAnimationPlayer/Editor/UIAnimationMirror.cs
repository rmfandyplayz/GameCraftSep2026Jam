// -----------------------------------------------------------------------------
// UI Animation Utility
//
// AI-GENERATED. Authored by Claude (Anthropic) via Claude Code, September 2026,
// to a written design brief by the project author. Not hand-written by the
// Twindrill Goose team. See README.md in the folder above for usage.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Turns an authored animation into the animation that undoes it, by rewriting the data
/// rather than by playing anything backwards. A mirrored Show is a real, editable Hide.
///
/// This is an authoring operation: it runs once, from the right-click menu, and what it
/// produces is ordinary authored data you can then tune. Nothing at runtime knows about
/// mirroring, which is why the ease and curve maths live here rather than on UIAnimationStep.
/// </summary>
internal static class UIAnimationMirror
{
    /// <summary>
    /// Mirrors an animation in place: groups run in the opposite order, staggers inside a
    /// group run the other way, and every step's endpoints and easing invert.
    /// Warns about any step whose forward start was undefined, since those cannot be
    /// mirrored exactly and are the ones worth looking at afterwards.
    /// </summary>
    public static void Mirror(UIAnimation animation, Object context)
    {
        List<UIAnimationStep> steps = animation.Steps;
        if (steps == null || steps.Count == 0) return;

        List<List<UIAnimationStep>> groups = BuildGroups(steps);

        for (int g = 0; g < groups.Count; g++)
        {
            MirrorGroupDelays(groups[g]);
        }

        var needsReview = new List<UIAnimationStep>();
        for (int i = 0; i < steps.Count; i++)
        {
            if (!MirrorStep(steps[i])) needsReview.Add(steps[i]);
        }

        // Reverse the order of the GROUPS, not of the raw list. Start is relative to the
        // step above, so reversing the list naively would scramble which steps are joined.
        groups.Reverse();
        steps.Clear();

        for (int g = 0; g < groups.Count; g++)
        {
            List<UIAnimationStep> group = groups[g];

            // Reverse inside the group too, so the whole list reads last-to-first. This is
            // purely how it reads: joined steps all offset from the same group start, so their
            // order in the list never affected timing, and MirrorGroupDelays has already
            // flipped the stagger. Without this an animation authored as one joined group -
            // the common case - appears not to mirror at all.
            group.Reverse();

            for (int s = 0; s < group.Count; s++)
            {
                group[s].Start = s == 0
                    ? UIAnimationStartMode.AfterPrevious
                    : UIAnimationStartMode.WithPrevious;

                steps.Add(group[s]);
            }
        }

        if (needsReview.Count > 0) Report(animation, needsReview, context);
    }

    /// <summary>
    /// Mirrors one step's endpoints and easing. Returns false when the step had no authored
    /// FROM, because then its forward starting value was whatever the property happened to
    /// hold and there is nothing exact to mirror it onto.
    /// </summary>
    public static bool MirrorStep(UIAnimationStep step)
    {
        if (step.Type == UIAnimationStepType.SetActive)
        {
            // A Show that switches something on becomes a Hide that switches it off. This is
            // the only reading under which a mirrored animation actually undoes the original.
            step.ActiveValue = !step.ActiveValue;
            return true;
        }

        // A clip has no reverse, and the sound still belongs at this point in the timeline.
        if (step.Type == UIAnimationStepType.PlaySound) return true;

        // Punch and shake return to where they started and carry their own internal easing,
        // so there is nothing to invert.
        if (UIAnimationStep.IsImpulse(step.Type)) return true;

        // An ease that decelerates into its end value should accelerate out of it going
        // the other way.
        if (step.UseCustomCurve) step.Curve = MirrorCurve(step.Curve);
        else step.EaseType = MirrorEase(step.EaseType);

        if (step.UseFrom)
        {
            // Both ends are declared, so the mirror is exact: swap them.
            Swap(ref step.FromMode, ref step.ToMode);
            Swap(ref step.FromVector, ref step.ToVector);
            Swap(ref step.FromFloat, ref step.ToFloat);
            Swap(ref step.FromColor, ref step.ToColor);
            return true;
        }

        if (step.ToMode == UIAnimationEndpointMode.Current)
        {
            // A relative nudge mirrors exactly by negating the offset.
            step.ToVector = -step.ToVector;
            step.ToFloat = -step.ToFloat;
            step.ToColor = Negate(step.ToColor);
            return true;
        }

        // No authored FROM. The one thing we do know is where the forward step ended, so the
        // mirror starts there; the resting value is the best available guess for where it
        // should land. Flagged, because that guess is the part worth checking.
        step.UseFrom = true;
        step.FromMode = step.ToMode;
        step.FromVector = step.ToVector;
        step.FromFloat = step.ToFloat;
        step.FromColor = step.ToColor;

        step.ToMode = UIAnimationEndpointMode.Baseline;
        step.ToVector = Vector3.zero;
        step.ToFloat = 0f;
        step.ToColor = new Color(0f, 0f, 0f, 0f);

        return false;
    }

    /// <summary>
    /// Splits the step list into join-groups: each group is one step that starts after the
    /// previous group, plus every step joined alongside it.
    /// </summary>
    private static List<List<UIAnimationStep>> BuildGroups(List<UIAnimationStep> steps)
    {
        var groups = new List<List<UIAnimationStep>>();

        for (int i = 0; i < steps.Count; i++)
        {
            // The first step always opens a group, whatever its Start says - that is how
            // UIAnimationPlayer lays the sequence out too.
            bool opensGroup = i == 0 || steps[i].Start == UIAnimationStartMode.AfterPrevious;

            if (opensGroup) groups.Add(new List<UIAnimationStep>());
            groups[groups.Count - 1].Add(steps[i]);
        }

        return groups;
    }

    /// <summary>
    /// Flips delays within a joined group so a stagger runs the other way: the step that
    /// arrived last is the first to leave. A group of one keeps its delay, because its gap
    /// belongs between groups and cannot be expressed on the step itself.
    /// </summary>
    private static void MirrorGroupDelays(List<UIAnimationStep> group)
    {
        if (group.Count < 2) return;

        float span = 0f;
        for (int i = 0; i < group.Count; i++)
        {
            span = Mathf.Max(span, group[i].TotalDuration);
        }

        for (int i = 0; i < group.Count; i++)
        {
            group[i].Delay = Mathf.Max(0f, span - group[i].TotalDuration);
        }
    }

    private static void Report(UIAnimation animation, List<UIAnimationStep> needsReview, Object context)
    {
        var text = new StringBuilder();

        text.Append("Mirrored animation '").Append(animation.Name).Append("'. ");
        text.Append(needsReview.Count == 1 ? "1 step had" : needsReview.Count + " steps had");
        text.Append(" Use From switched off, so there was no authored start to mirror onto. ");
        text.Append("Their To is now the resting value, which is the part worth checking: ");

        for (int i = 0; i < needsReview.Count; i++)
        {
            if (i > 0) text.Append(", ");

            text.Append("step ").Append(animation.Steps.IndexOf(needsReview[i]));
            text.Append(" (").Append(needsReview[i].Type).Append(")");
        }

        Debug.LogWarning(text.ToString(), context);
    }

    /// <summary>
    /// The time-mirror of an easing preset: an ease that decelerates into its end value should
    /// accelerate out of it going the other way.
    /// Linear and the InOut / OutIn families are already symmetric, so they map to themselves.
    /// </summary>
    private static Ease MirrorEase(Ease ease)
    {
        switch (ease)
        {
            case Ease.InSine: return Ease.OutSine;
            case Ease.OutSine: return Ease.InSine;
            case Ease.InQuad: return Ease.OutQuad;
            case Ease.OutQuad: return Ease.InQuad;
            case Ease.InCubic: return Ease.OutCubic;
            case Ease.OutCubic: return Ease.InCubic;
            case Ease.InQuart: return Ease.OutQuart;
            case Ease.OutQuart: return Ease.InQuart;
            case Ease.InQuint: return Ease.OutQuint;
            case Ease.OutQuint: return Ease.InQuint;
            case Ease.InExpo: return Ease.OutExpo;
            case Ease.OutExpo: return Ease.InExpo;
            case Ease.InCirc: return Ease.OutCirc;
            case Ease.OutCirc: return Ease.InCirc;
            case Ease.InElastic: return Ease.OutElastic;
            case Ease.OutElastic: return Ease.InElastic;
            case Ease.InBack: return Ease.OutBack;
            case Ease.OutBack: return Ease.InBack;
            case Ease.InBounce: return Ease.OutBounce;
            case Ease.OutBounce: return Ease.InBounce;
            case Ease.InFlash: return Ease.OutFlash;
            case Ease.OutFlash: return Ease.InFlash;

            default: return ease;
        }
    }

    /// <summary>
    /// A custom easing curve flipped through both axes: g(t) = 1 - f(1 - t). The slope at a
    /// mirrored key is unchanged but time runs the other way, so in and out tangents swap.
    /// Assumes the curve spans 0..1, which is the range DOTween evaluates an ease curve over.
    /// </summary>
    private static AnimationCurve MirrorCurve(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0) return curve;

        Keyframe[] source = curve.keys;
        var keys = new Keyframe[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            Keyframe k = source[source.Length - 1 - i];

            keys[i] = new Keyframe(1f - k.time, 1f - k.value, k.outTangent, k.inTangent,
                                   k.outWeight, k.inWeight);
            keys[i].weightedMode = SwapWeightedMode(k.weightedMode);
        }

        return new AnimationCurve(keys);
    }

    private static WeightedMode SwapWeightedMode(WeightedMode mode)
    {
        if (mode == WeightedMode.In) return WeightedMode.Out;
        if (mode == WeightedMode.Out) return WeightedMode.In;
        return mode;
    }

    private static Color Negate(Color c)
    {
        // Color has no unary minus.
        return new Color(-c.r, -c.g, -c.b, -c.a);
    }

    private static void Swap<T>(ref T a, ref T b)
    {
        T temp = a;
        a = b;
        b = temp;
    }
}
