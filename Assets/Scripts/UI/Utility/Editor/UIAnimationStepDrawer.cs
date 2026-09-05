// -----------------------------------------------------------------------------
// UI Animation Utility
//
// AI-GENERATED. Authored by Claude (Anthropic) via Claude Code, September 2026,
// to a written design brief by the project author. Not hand-written by the
// Twindrill Goose team. See Assets/Scripts/UI/Utility/README.md for usage.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

/// <summary>
/// Collapses a UIAnimationStep down to only the fields its selected Type actually uses.
/// Without this the flat step class shows every value field at once and authoring is painful.
/// </summary>
[CustomPropertyDrawer(typeof(UIAnimationStep))]
public class UIAnimationStepDrawer : PropertyDrawer
{
    private const float Pad = 2f;
    private const float LabelWidth = 74f;
    private const float ModeWidth = 78f;

    private static readonly string[] AbsoluteBaselineOnly = { "Absolute", "Baseline" };

    /// <summary>Tracks vertical layout. Run once to measure, once to draw.</summary>
    private struct Layout
    {
        public Rect Area;
        public float Used;
        public bool Draw;

        public Rect Line()
        {
            Rect r = new Rect(Area.x, Area.y + Used, Area.width, EditorGUIUtility.singleLineHeight);
            Used += EditorGUIUtility.singleLineHeight + Pad;
            return r;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        Layout layout = new Layout { Area = new Rect(0f, 0f, 100f, 0f), Draw = false };
        Render(ref layout, property);
        return layout.Used;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Layout layout = new Layout { Area = position, Draw = true };
        Render(ref layout, property);

        EditorGUI.EndProperty();
    }

    private void Render(ref Layout layout, SerializedProperty property)
    {
        SerializedProperty type = property.FindPropertyRelative("Type");
        SerializedProperty start = property.FindPropertyRelative("Start");

        var stepType = (UIAnimationStepType)type.enumValueIndex;
        var startMode = (UIAnimationStartMode)start.enumValueIndex;

        Rect header = layout.Line();
        if (layout.Draw)
        {
            property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, Summary(property, stepType, startMode), true);
        }

        if (!property.isExpanded) return;

        layout.Area = new Rect(layout.Area.x + 12f, layout.Area.y, layout.Area.width - 12f, layout.Area.height);

        Field(ref layout, type);
        Field(ref layout, start);

        DrawTargetSlot(ref layout, property, stepType);

        bool impulse = UIAnimationStep.IsImpulse(stepType);

        if (stepType == UIAnimationStepType.SetActive)
        {
            Field(ref layout, property.FindPropertyRelative("ActiveValue"), "Set Active To");
            Field(ref layout, property.FindPropertyRelative("Delay"));
            return;
        }

        if (stepType == UIAnimationStepType.MaterialFloat || stepType == UIAnimationStepType.MaterialColor)
        {
            Field(ref layout, property.FindPropertyRelative("ShaderProperty"));
        }

        Field(ref layout, property.FindPropertyRelative("Duration"));
        Field(ref layout, property.FindPropertyRelative("Delay"));

        if (!impulse)
        {
            SerializedProperty useCurve = property.FindPropertyRelative("UseCustomCurve");
            Field(ref layout, useCurve, "Use Custom Curve");

            if (useCurve.boolValue) Field(ref layout, property.FindPropertyRelative("Curve"));
            else Field(ref layout, property.FindPropertyRelative("EaseType"), "Ease");
        }

        if (impulse)
        {
            DrawImpulseFields(ref layout, property, stepType);
            return;
        }

        UIAnimationValueKind kind = UIAnimationStep.ValueKindOf(stepType);
        SerializedProperty useFrom = property.FindPropertyRelative("UseFrom");
        Field(ref layout, useFrom);

        if (useFrom.boolValue)
        {
            DrawEndpoint(ref layout, property, "From", "FromMode", kind, stepType, true);
        }

        // "Current" on the TO side means DOTween relative, which is contradictory with an
        // explicit FROM value - so it is only offered when Use From is off.
        DrawEndpoint(ref layout, property, "To", "ToMode", kind, stepType, !useFrom.boolValue);

        if (UsesSnapping(stepType)) Field(ref layout, property.FindPropertyRelative("Snapping"));
    }

    private void DrawTargetSlot(ref Layout layout, SerializedProperty property, UIAnimationStepType stepType)
    {
        string field;
        string label;

        switch (UIAnimationStep.TargetKindOf(stepType))
        {
            case UIAnimationTargetKind.CanvasGroup:
                field = "CanvasGroupTarget";
                label = "Canvas Group";
                break;

            case UIAnimationTargetKind.Graphic:
                field = "GraphicTarget";
                label = "Graphic";
                break;

            case UIAnimationTargetKind.Material:
                field = "MaterialTarget";
                label = "Material Inst.";
                break;

            case UIAnimationTargetKind.GameObject:
                field = "ActiveTarget";
                label = "Game Object";
                break;

            default:
                field = "RectTarget";
                label = "Rect Transform";
                break;
        }

        SerializedProperty target = property.FindPropertyRelative(field);
        Rect r = layout.Line();
        if (!layout.Draw) return;

        EditorGUI.PropertyField(r, target, new GUIContent(label, target.tooltip));
    }

    private void DrawImpulseFields(ref Layout layout, SerializedProperty property, UIAnimationStepType stepType)
    {
        if (stepType == UIAnimationStepType.ShakeAnchoredPosition)
        {
            Field(ref layout, property.FindPropertyRelative("ToFloat"), "Strength");
            Field(ref layout, property.FindPropertyRelative("Vibrato"));
            Field(ref layout, property.FindPropertyRelative("Randomness"));
        }
        else
        {
            Field(ref layout, property.FindPropertyRelative("ToVector"), "Punch");
            Field(ref layout, property.FindPropertyRelative("Vibrato"));
            Field(ref layout, property.FindPropertyRelative("Elasticity"));
        }

        if (UsesSnapping(stepType)) Field(ref layout, property.FindPropertyRelative("Snapping"));
    }

    private void DrawEndpoint(ref Layout layout, SerializedProperty property, string label, string modeField,
        UIAnimationValueKind kind, UIAnimationStepType stepType, bool allowCurrent)
    {
        Rect r = layout.Line();
        if (!layout.Draw) return;

        SerializedProperty mode = property.FindPropertyRelative(modeField);
        SerializedProperty value = property.FindPropertyRelative(ValueFieldName(label, kind));

        var labelRect = new Rect(r.x, r.y, LabelWidth, r.height);
        var modeRect = new Rect(r.x + LabelWidth, r.y, ModeWidth, r.height);
        var valueRect = new Rect(modeRect.xMax + 4f, r.y, Mathf.Max(40f, r.xMax - modeRect.xMax - 4f), r.height);

        EditorGUI.LabelField(labelRect, new GUIContent(label, mode.tooltip));

        if (allowCurrent)
        {
            EditorGUI.PropertyField(modeRect, mode, GUIContent.none);
        }
        else
        {
            int index = Mathf.Clamp(mode.enumValueIndex, 0, AbsoluteBaselineOnly.Length - 1);
            index = EditorGUI.Popup(modeRect, index, AbsoluteBaselineOnly);
            mode.enumValueIndex = index;
        }

        if (kind == UIAnimationValueKind.Vector && IsTwoDimensional(stepType))
        {
            Vector3 current = value.vector3Value;
            Vector2 edited = EditorGUI.Vector2Field(valueRect, GUIContent.none, current);
            value.vector3Value = new Vector3(edited.x, edited.y, current.z);
        }
        else
        {
            EditorGUI.PropertyField(valueRect, value, GUIContent.none);
        }
    }

    private static string ValueFieldName(string label, UIAnimationValueKind kind)
    {
        string prefix = label == "From" ? "From" : "To";

        switch (kind)
        {
            case UIAnimationValueKind.Float: return prefix + "Float";
            case UIAnimationValueKind.Color: return prefix + "Color";
            default: return prefix + "Vector";
        }
    }

    private static bool IsTwoDimensional(UIAnimationStepType type)
    {
        return type == UIAnimationStepType.AnchoredPosition
            || type == UIAnimationStepType.PunchAnchoredPosition;
    }

    private static bool UsesSnapping(UIAnimationStepType type)
    {
        return type == UIAnimationStepType.AnchoredPosition
            || type == UIAnimationStepType.LocalPosition
            || type == UIAnimationStepType.PunchAnchoredPosition
            || type == UIAnimationStepType.ShakeAnchoredPosition;
    }

    private void Field(ref Layout layout, SerializedProperty property, string label = null)
    {
        Rect r = layout.Line();
        if (!layout.Draw || property == null) return;

        // Keep the field's [Tooltip] even when the drawer overrides its display name.
        if (label == null) EditorGUI.PropertyField(r, property);
        else EditorGUI.PropertyField(r, property, new GUIContent(label, property.tooltip));
    }

    private static string Summary(SerializedProperty property, UIAnimationStepType type, UIAnimationStartMode start)
    {
        string prefix = start == UIAnimationStartMode.WithPrevious ? "with" : "then";
        SerializedProperty typeProperty = property.FindPropertyRelative("Type");

        // Use Unity's prettified enum names so the header matches the dropdowns below it.
        string typeName = typeProperty.enumValueIndex >= 0
            ? typeProperty.enumDisplayNames[typeProperty.enumValueIndex]
            : type.ToString();

        if (type == UIAnimationStepType.SetActive)
        {
            bool value = property.FindPropertyRelative("ActiveValue").boolValue;
            return prefix + "   " + typeName + " " + (value ? "on" : "off");
        }

        float duration = property.FindPropertyRelative("Duration").floatValue;
        string tail = duration.ToString("0.##") + "s";

        if (!UIAnimationStep.IsImpulse(type))
        {
            if (property.FindPropertyRelative("UseCustomCurve").boolValue)
            {
                tail += "  Custom Curve";
            }
            else
            {
                SerializedProperty ease = property.FindPropertyRelative("EaseType");
                if (ease.enumValueIndex >= 0) tail += "  " + ease.enumDisplayNames[ease.enumValueIndex];
            }
        }

        return prefix + "   " + typeName + "   " + tail;
    }
}
