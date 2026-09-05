// -----------------------------------------------------------------------------
// UI Animation Utility
//
// AI-GENERATED. Authored by Claude (Anthropic) via Claude Code, September 2026,
// to a written design brief by the project author. Not hand-written by the
// Twindrill Goose team. See Assets/Scripts/UI/Utility/README.md for usage.
// -----------------------------------------------------------------------------

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// What a single animation step drives. The framework never interprets these
/// semantically - it only knows how to build a DOTween tween for each one.
/// </summary>
public enum UIAnimationStepType
{
    AnchoredPosition,
    LocalPosition,
    Scale,
    Rotation,
    CanvasGroupAlpha,
    GraphicColor,
    GraphicAlpha,
    MaterialFloat,
    MaterialColor,
    PunchScale,
    PunchAnchoredPosition,
    ShakeAnchoredPosition,
    SetActive,
}

/// <summary>How a FROM/TO endpoint value is resolved at build time.</summary>
public enum UIAnimationEndpointMode
{
    /// <summary>Use the authored value exactly as typed.</summary>
    Absolute,

    /// <summary>Resting value captured at Awake, plus the authored value as an offset.</summary>
    Baseline,

    /// <summary>Value when the tween starts, plus the authored value as an offset (DOTween relative).</summary>
    Current,
}

/// <summary>Whether a step is appended after the previous one or joined alongside it.</summary>
public enum UIAnimationStartMode
{
    AfterPrevious,
    WithPrevious,
}

/// <summary>Which value fields a step type actually uses. Drives the inspector drawer.</summary>
public enum UIAnimationValueKind
{
    None,
    Float,
    Vector,
    Color,
}

/// <summary>Which target slot a step type needs. Drives the inspector drawer.</summary>
public enum UIAnimationTargetKind
{
    Rect,
    CanvasGroup,
    Graphic,
    Material,
    GameObject,
}

/// <summary>
/// One tween inside a named animation. Flat and serializable on purpose - a custom
/// PropertyDrawer hides whichever fields the selected Type does not use.
/// </summary>
[Serializable]
public class UIAnimationStep
{
    public UIAnimationStepType Type = UIAnimationStepType.CanvasGroupAlpha;
    public UIAnimationStartMode Start = UIAnimationStartMode.AfterPrevious;

    // Target slots. Leave empty to target the UIAnimationPlayer own GameObject.
    public RectTransform RectTarget;
    public CanvasGroup CanvasGroupTarget;
    public Graphic GraphicTarget;
    public UIMaterialInstance MaterialTarget;
    public GameObject ActiveTarget;

    // Timing.
    public float Duration = 0.25f;
    public float Delay;
    public Ease EaseType = Ease.OutQuad;

    // Endpoints.
    public bool UseFrom;
    public UIAnimationEndpointMode FromMode = UIAnimationEndpointMode.Absolute;
    public UIAnimationEndpointMode ToMode = UIAnimationEndpointMode.Absolute;
    public Vector3 FromVector;
    public Vector3 ToVector;
    public float FromFloat;
    public float ToFloat = 1f;
    public Color FromColor = Color.white;
    public Color ToColor = Color.white;

    // Type specific.
    public string ShaderProperty = "_Progress";
    public bool ActiveValue = true;
    public int Vibrato = 10;
    public float Elasticity = 1f;
    public float Randomness = 90f;
    public bool Snapping;

    // Runtime only. Never serialized, so authored data is never mutated by play mode.
    [NonSerialized] private RectTransform rect;
    [NonSerialized] private CanvasGroup canvasGroup;
    [NonSerialized] private Graphic graphic;
    [NonSerialized] private UIMaterialInstance materialInstance;
    [NonSerialized] private GameObject activeObject;

    [NonSerialized] private Vector3 baselineVector;
    [NonSerialized] private float baselineFloat;
    [NonSerialized] private Color baselineColor;
    [NonSerialized] private int shaderPropertyId;

    /// <summary>Time this step occupies, used to lay out Append/Join positions.</summary>
    public float TotalDuration
    {
        get { return Type == UIAnimationStepType.SetActive ? Delay : Delay + Mathf.Max(0f, Duration); }
    }

    public static UIAnimationValueKind ValueKindOf(UIAnimationStepType type)
    {
        switch (type)
        {
            case UIAnimationStepType.CanvasGroupAlpha:
            case UIAnimationStepType.GraphicAlpha:
            case UIAnimationStepType.MaterialFloat:
            case UIAnimationStepType.ShakeAnchoredPosition:
                return UIAnimationValueKind.Float;

            case UIAnimationStepType.GraphicColor:
            case UIAnimationStepType.MaterialColor:
                return UIAnimationValueKind.Color;

            case UIAnimationStepType.SetActive:
                return UIAnimationValueKind.None;

            default:
                return UIAnimationValueKind.Vector;
        }
    }

    public static UIAnimationTargetKind TargetKindOf(UIAnimationStepType type)
    {
        switch (type)
        {
            case UIAnimationStepType.CanvasGroupAlpha:
                return UIAnimationTargetKind.CanvasGroup;

            case UIAnimationStepType.GraphicColor:
            case UIAnimationStepType.GraphicAlpha:
                return UIAnimationTargetKind.Graphic;

            case UIAnimationStepType.MaterialFloat:
            case UIAnimationStepType.MaterialColor:
                return UIAnimationTargetKind.Material;

            case UIAnimationStepType.SetActive:
                return UIAnimationTargetKind.GameObject;

            default:
                return UIAnimationTargetKind.Rect;
        }
    }

    /// <summary>True for punch/shake, which have no meaningful FROM/TO pair.</summary>
    public static bool IsImpulse(UIAnimationStepType type)
    {
        return type == UIAnimationStepType.PunchScale
            || type == UIAnimationStepType.PunchAnchoredPosition
            || type == UIAnimationStepType.ShakeAnchoredPosition;
    }

    /// <summary>Fills empty target slots from the player own GameObject. Call before CaptureBaseline.</summary>
    public void Resolve(GameObject owner)
    {
        switch (TargetKindOf(Type))
        {
            case UIAnimationTargetKind.Rect:
                rect = RectTarget != null ? RectTarget : owner.GetComponent<RectTransform>();
                break;

            case UIAnimationTargetKind.CanvasGroup:
                canvasGroup = CanvasGroupTarget != null ? CanvasGroupTarget : owner.GetComponent<CanvasGroup>();
                break;

            case UIAnimationTargetKind.Graphic:
                graphic = GraphicTarget != null ? GraphicTarget : owner.GetComponent<Graphic>();
                break;

            case UIAnimationTargetKind.Material:
                materialInstance = MaterialTarget != null ? MaterialTarget : owner.GetComponent<UIMaterialInstance>();
                shaderPropertyId = Shader.PropertyToID(ShaderProperty);
                break;

            case UIAnimationTargetKind.GameObject:
                activeObject = ActiveTarget != null ? ActiveTarget : owner;
                break;
        }
    }

    /// <summary>
    /// Records the target resting value. Called once at Awake before anything animates,
    /// so every step touching the same target agrees on the same baseline.
    /// </summary>
    public void CaptureBaseline()
    {
        switch (Type)
        {
            case UIAnimationStepType.AnchoredPosition:
                if (rect != null) baselineVector = rect.anchoredPosition;
                break;

            case UIAnimationStepType.LocalPosition:
                if (rect != null) baselineVector = rect.localPosition;
                break;

            case UIAnimationStepType.Scale:
                if (rect != null) baselineVector = rect.localScale;
                break;

            case UIAnimationStepType.Rotation:
                if (rect != null) baselineVector = rect.localEulerAngles;
                break;

            case UIAnimationStepType.CanvasGroupAlpha:
                if (canvasGroup != null) baselineFloat = canvasGroup.alpha;
                break;

            case UIAnimationStepType.GraphicColor:
                if (graphic != null) baselineColor = graphic.color;
                break;

            case UIAnimationStepType.GraphicAlpha:
                if (graphic != null) baselineFloat = graphic.color.a;
                break;

            case UIAnimationStepType.MaterialFloat:
                if (HasMaterial()) baselineFloat = materialInstance.Material.GetFloat(shaderPropertyId);
                break;

            case UIAnimationStepType.MaterialColor:
                if (HasMaterial()) baselineColor = materialInstance.Material.GetColor(shaderPropertyId);
                break;
        }
    }

    /// <summary>Writes this step FROM value straight to the target, without playing anything.</summary>
    public void ApplyFromValue()
    {
        if (!UseFrom || IsImpulse(Type) || FromMode == UIAnimationEndpointMode.Current) return;

        switch (Type)
        {
            case UIAnimationStepType.AnchoredPosition:
                if (rect != null) rect.anchoredPosition = ResolveVector(FromMode, FromVector);
                break;

            case UIAnimationStepType.LocalPosition:
                if (rect != null) rect.localPosition = ResolveVector(FromMode, FromVector);
                break;

            case UIAnimationStepType.Scale:
                if (rect != null) rect.localScale = ResolveVector(FromMode, FromVector);
                break;

            case UIAnimationStepType.Rotation:
                if (rect != null) rect.localEulerAngles = ResolveVector(FromMode, FromVector);
                break;

            case UIAnimationStepType.CanvasGroupAlpha:
                if (canvasGroup != null) canvasGroup.alpha = ResolveFloat(FromMode, FromFloat);
                break;

            case UIAnimationStepType.GraphicColor:
                if (graphic != null) graphic.color = ResolveColor(FromMode, FromColor);
                break;

            case UIAnimationStepType.GraphicAlpha:
                if (graphic != null)
                {
                    Color c = graphic.color;
                    c.a = ResolveFloat(FromMode, FromFloat);
                    graphic.color = c;
                }
                break;

            case UIAnimationStepType.MaterialFloat:
                if (HasMaterial()) materialInstance.Material.SetFloat(shaderPropertyId, ResolveFloat(FromMode, FromFloat));
                break;

            case UIAnimationStepType.MaterialColor:
                if (HasMaterial()) materialInstance.Material.SetColor(shaderPropertyId, ResolveColor(FromMode, FromColor));
                break;
        }
    }

    /// <summary>
    /// Builds a fully configured tween. Everything (From/Ease/Delay/Relative) is applied
    /// HERE, before the caller hands it to Append/Join - DOTween silently ignores those
    /// calls once a tween has been inserted into a Sequence.
    /// Returns null for SetActive steps (the player turns those into a callback) and for
    /// steps whose target is missing.
    /// </summary>
    public Tween BuildTween(bool applyFromImmediately, string context)
    {
        if (Type == UIAnimationStepType.SetActive) return null;
        if (!HasTarget(context)) return null;

        bool relative = !UseFrom && ToMode == UIAnimationEndpointMode.Current;
        bool useFrom = UseFrom && !IsImpulse(Type) && FromMode != UIAnimationEndpointMode.Current;
        Tween tween;

        switch (Type)
        {
            case UIAnimationStepType.AnchoredPosition:
            {
                var t = rect.DOAnchorPos(ResolveVector(ToMode, ToVector), Duration, Snapping);
                if (useFrom) t.From((Vector2)ResolveVector(FromMode, FromVector), applyFromImmediately);
                else if (relative) t.SetRelative(true);
                tween = t;
                break;
            }

            case UIAnimationStepType.LocalPosition:
            {
                var t = rect.DOLocalMove(ResolveVector(ToMode, ToVector), Duration, Snapping);
                if (useFrom) t.From(ResolveVector(FromMode, FromVector), applyFromImmediately);
                else if (relative) t.SetRelative(true);
                tween = t;
                break;
            }

            case UIAnimationStepType.Scale:
            {
                var t = rect.DOScale(ResolveVector(ToMode, ToVector), Duration);
                if (useFrom) t.From(ResolveVector(FromMode, FromVector), applyFromImmediately);
                else if (relative) t.SetRelative(true);
                tween = t;
                break;
            }

            case UIAnimationStepType.Rotation:
            {
                var t = rect.DOLocalRotate(ResolveVector(ToMode, ToVector), Duration, RotateMode.FastBeyond360);
                if (useFrom) t.From(ResolveVector(FromMode, FromVector), applyFromImmediately);
                else if (relative) t.SetRelative(true);
                tween = t;
                break;
            }

            case UIAnimationStepType.CanvasGroupAlpha:
            {
                var t = canvasGroup.DOFade(ResolveFloat(ToMode, ToFloat), Duration);
                if (useFrom) t.From(ResolveFloat(FromMode, FromFloat), applyFromImmediately);
                else if (relative) t.SetRelative(true);
                tween = t;
                break;
            }

            case UIAnimationStepType.GraphicColor:
            {
                var t = graphic.DOColor(ResolveColor(ToMode, ToColor), Duration);
                if (useFrom) t.From(ResolveColor(FromMode, FromColor), applyFromImmediately);
                else if (relative) t.SetRelative(true);
                tween = t;
                break;
            }

            case UIAnimationStepType.GraphicAlpha:
            {
                var t = graphic.DOFade(ResolveFloat(ToMode, ToFloat), Duration);
                if (useFrom) t.From(ResolveFloat(FromMode, FromFloat), applyFromImmediately);
                else if (relative) t.SetRelative(true);
                tween = t;
                break;
            }

            case UIAnimationStepType.MaterialFloat:
            {
                var t = materialInstance.Material.DOFloat(ResolveFloat(ToMode, ToFloat), shaderPropertyId, Duration);
                if (useFrom) t.From(ResolveFloat(FromMode, FromFloat), applyFromImmediately);
                else if (relative) t.SetRelative(true);
                tween = t;
                break;
            }

            case UIAnimationStepType.MaterialColor:
            {
                var t = materialInstance.Material.DOColor(ResolveColor(ToMode, ToColor), shaderPropertyId, Duration);
                if (useFrom) t.From(ResolveColor(FromMode, FromColor), applyFromImmediately);
                else if (relative) t.SetRelative(true);
                tween = t;
                break;
            }

            case UIAnimationStepType.PunchScale:
                tween = rect.DOPunchScale(ToVector, Duration, Vibrato, Elasticity);
                break;

            case UIAnimationStepType.PunchAnchoredPosition:
                tween = rect.DOPunchAnchorPos(ToVector, Duration, Vibrato, Elasticity, Snapping);
                break;

            case UIAnimationStepType.ShakeAnchoredPosition:
                tween = rect.DOShakeAnchorPos(Duration, ToFloat, Vibrato, Randomness, Snapping);
                break;

            default:
                return null;
        }

        // Punch and shake carry their own internal easing; overriding it looks wrong.
        if (!IsImpulse(Type)) tween.SetEase(EaseType);
        if (Delay > 0f) tween.SetDelay(Delay);
        return tween;
    }

    /// <summary>Runs a SetActive step. The player calls this from a sequence callback.</summary>
    public void ApplyActiveValue()
    {
        if (activeObject != null) activeObject.SetActive(ActiveValue);
    }

    private Vector3 ResolveVector(UIAnimationEndpointMode mode, Vector3 value)
    {
        return mode == UIAnimationEndpointMode.Baseline ? baselineVector + value : value;
    }

    private float ResolveFloat(UIAnimationEndpointMode mode, float value)
    {
        return mode == UIAnimationEndpointMode.Baseline ? baselineFloat + value : value;
    }

    private Color ResolveColor(UIAnimationEndpointMode mode, Color value)
    {
        return mode == UIAnimationEndpointMode.Baseline ? baselineColor + value : value;
    }

    private bool HasMaterial()
    {
        return materialInstance != null && materialInstance.Material != null;
    }

    private bool HasTarget(string context)
    {
        switch (TargetKindOf(Type))
        {
            case UIAnimationTargetKind.Rect:
                if (rect != null) return true;
                break;

            case UIAnimationTargetKind.CanvasGroup:
                if (canvasGroup != null) return true;
                break;

            case UIAnimationTargetKind.Graphic:
                if (graphic != null) return true;
                break;

            case UIAnimationTargetKind.Material:
                if (HasMaterial()) return true;
                break;

            case UIAnimationTargetKind.GameObject:
                if (activeObject != null) return true;
                break;
        }

        Debug.LogWarning(context + ": " + Type + " step has no " + TargetKindOf(Type) + " target and was skipped.");
        return false;
    }
}
