// -----------------------------------------------------------------------------
// UI Animation Utility
//
// AI-GENERATED. Authored by Claude (Anthropic) via Claude Code, September 2026,
// to a written design brief by the project author. Not hand-written by the
// Twindrill Goose team. See README.md in the folder above for usage.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds play-mode Play/Stop buttons so animation timing can be tuned without writing test code.
/// </summary>
[CustomEditor(typeof(UIAnimationPlayer))]
[CanEditMultipleObjects]
public class UIAnimationPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (targets.Length > 1) return;

        var player = (UIAnimationPlayer)target;

        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Right-click an animation or step header to copy and paste it, here or on another object.\n" +
                "Enter play mode to preview animations.", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        var animations = player.EditorAnimations;
        for (int i = 0; i < animations.Count; i++)
        {
            string animationName = animations[i].Name;
            if (string.IsNullOrEmpty(animationName)) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(animationName, GUILayout.MinWidth(60f));

            if (GUILayout.Button("Play", GUILayout.Width(44f))) player.Play(animationName);
            if (GUILayout.Button("Start", GUILayout.Width(44f))) player.ApplyFromState(animationName);
            if (GUILayout.Button("Stop", GUILayout.Width(44f))) player.Stop(animationName);

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Stop All")) player.StopAll();

        Repaint();
    }
}
