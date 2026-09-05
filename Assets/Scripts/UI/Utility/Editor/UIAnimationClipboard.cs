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
/// Right-click copy/paste for whole animations and for individual steps, across GameObjects.
///
/// Unity's own generic Copy/Paste on a serialized property drops object references, which for
/// this data means every target slot comes back empty. EditorJsonUtility keeps them, so a
/// pasted step still points at the Graphic or AudioClip it was copied from.
///
/// Entries appear on the property context menu:
///   right-click an animation header  -> Copy Animation / Paste Animation (overwrite)
///   right-click a step header        -> Copy Step / Paste Step (overwrite)
///   right-click the Animations list  -> Paste Animation (add to end)
///   right-click the Steps list       -> Paste Step (add to end)
/// </summary>
[InitializeOnLoad]
internal static class UIAnimationClipboard
{
    private const string AnimationTypeName = "UIAnimation";
    private const string StepTypeName = "UIAnimationStep";

    // Held as JSON rather than as a live object, so editing the source afterwards
    // does not retroactively change what is on the clipboard.
    private static string animationJson;
    private static string stepJson;

    static UIAnimationClipboard()
    {
        EditorApplication.contextualPropertyMenu -= OnContextMenu;
        EditorApplication.contextualPropertyMenu += OnContextMenu;
    }

    private static void OnContextMenu(GenericMenu menu, SerializedProperty property)
    {
        // Guards out strings, which also report isArray == true.
        if (property.propertyType != SerializedPropertyType.Generic) return;

        // The property handed to this callback is only valid for the duration of the call,
        // and menu items run later. Copy it so the deferred handler has something to use.
        SerializedProperty captured = property.Copy();

        if (property.isArray)
        {
            if (property.arrayElementType == AnimationTypeName) AddListEntry(menu, captured, true);
            else if (property.arrayElementType == StepTypeName) AddListEntry(menu, captured, false);
            return;
        }

        if (property.type == AnimationTypeName) AddElementEntries(menu, captured, true);
        else if (property.type == StepTypeName) AddElementEntries(menu, captured, false);
    }

    private static void AddElementEntries(GenericMenu menu, SerializedProperty element, bool isAnimation)
    {
        string noun = isAnimation ? "Animation" : "Step";

        menu.AddSeparator(string.Empty);
        menu.AddItem(new GUIContent("Copy " + noun), false, () => Copy(element, isAnimation));

        var paste = new GUIContent("Paste " + noun + " (overwrite)");

        if (HasCopy(isAnimation)) menu.AddItem(paste, false, () => Paste(element, isAnimation));
        else menu.AddDisabledItem(paste);
    }

    private static void AddListEntry(GenericMenu menu, SerializedProperty list, bool isAnimation)
    {
        var paste = new GUIContent("Paste " + (isAnimation ? "Animation" : "Step") + " (add to end)");

        menu.AddSeparator(string.Empty);

        if (HasCopy(isAnimation)) menu.AddItem(paste, false, () => Append(list, isAnimation));
        else menu.AddDisabledItem(paste);
    }

    private static bool HasCopy(bool isAnimation)
    {
        return !string.IsNullOrEmpty(isAnimation ? animationJson : stepJson);
    }

    private static void Copy(SerializedProperty element, bool isAnimation)
    {
        string json = EditorJsonUtility.ToJson(element.boxedValue);

        if (isAnimation) animationJson = json;
        else stepJson = json;
    }

    private static void Paste(SerializedProperty element, bool isAnimation)
    {
        object value = Rebuild(isAnimation);
        if (value == null) return;

        element.boxedValue = value;
        element.serializedObject.ApplyModifiedProperties();
    }

    private static void Append(SerializedProperty list, bool isAnimation)
    {
        object value = Rebuild(isAnimation);
        if (value == null) return;

        int index = list.arraySize;
        list.arraySize = index + 1;
        list.GetArrayElementAtIndex(index).boxedValue = value;

        // Play() resolves names through a dictionary where the first duplicate wins, so a
        // pasted animation keeping its original name would be silently unreachable.
        if (isAnimation) MakeNameUnique(list, index);

        list.serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Deserializes the clipboard onto a fresh instance. Starting from `new` rather than an
    /// empty object means any field the JSON happens not to carry keeps its C# default.
    /// </summary>
    private static object Rebuild(bool isAnimation)
    {
        if (!HasCopy(isAnimation)) return null;

        if (isAnimation)
        {
            var animation = new UIAnimation();
            EditorJsonUtility.FromJsonOverwrite(animationJson, animation);
            return animation;
        }

        var step = new UIAnimationStep();
        EditorJsonUtility.FromJsonOverwrite(stepJson, step);
        return step;
    }

    private static void MakeNameUnique(SerializedProperty list, int index)
    {
        SerializedProperty nameProperty = list.GetArrayElementAtIndex(index).FindPropertyRelative("Name");
        if (nameProperty == null || string.IsNullOrEmpty(nameProperty.stringValue)) return;

        string baseName = nameProperty.stringValue;
        string candidate = baseName;
        int suffix = 1;

        while (NameTaken(list, index, candidate))
        {
            suffix++;
            candidate = baseName + " " + suffix;
        }

        nameProperty.stringValue = candidate;
    }

    private static bool NameTaken(SerializedProperty list, int skipIndex, string candidate)
    {
        for (int i = 0; i < list.arraySize; i++)
        {
            if (i == skipIndex) continue;

            SerializedProperty other = list.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
            if (other != null && other.stringValue == candidate) return true;
        }

        return false;
    }
}
