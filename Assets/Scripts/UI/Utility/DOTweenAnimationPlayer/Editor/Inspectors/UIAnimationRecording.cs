// -----------------------------------------------------------------------------
// UI Animation Utility
//
// AI-GENERATED. Authored by Claude (Anthropic) via Claude Code, October 2026,
// to a written design brief by the project author.
// See README.md in the folder above for usage.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace rmf_claude.DOTweenUI
{
    /// <summary>A value read from the scene for a From or To row, in the row's own terms, ready to store.</summary>
    internal struct UIAnimationReading
    {
        public UIAnimationValueKind Kind;
        public bool TwoDimensional;

        /// <summary>A number in x, a vector in xyz, a colour as rgba.</summary>
        public Vector4 Number;

        public string Text;
    }

    /// <summary>
    /// Recording: the record button at the end of every From and To row in a pop-up animation window
    /// (UIAnimationWindow), which copies a value out of the scene into the row, and the session it starts.
    ///
    /// The first click starts a session. It holds a preview open on the player (UIAnimationPreview.Hold)
    /// - a new one starts where things stand, so nothing moves - and that preview captures every resting
    /// value before anything is posed: Baseline measures from those, and they are what goes back at the
    /// end. While it runs, the row shows what it will become, live, and more rows can be added. The user
    /// poses the objects any way they like - the Move tool, the Inspector - and Finish writes every row
    /// and puts the objects back; Cancel only puts them back. Closing the window is Finish.
    ///
    /// A row stores whatever its mode needs for the step to land exactly where the object was posed: the
    /// value itself for Absolute, its difference from the resting value for Baseline, its difference from
    /// where the step starts for Current (UIAnimationSimulation works that out). The mode is never changed.
    ///
    /// A Custom Property's getter is arbitrary code, and everything on screen redraws constantly, so its
    /// value is only read on Finish - which is a click, like the old button was.
    ///
    /// Finish and Cancel fold everything since the preview captured the resting state - the posing, and
    /// what Finish wrote - into one Undo step. Undoing a single pose after the objects were put back would
    /// otherwise move that object back to where it was posed from, out of the preview, for good.
    /// </summary>
    [InitializeOnLoad]
    internal static class UIAnimationRecording
    {
        /// <summary>A row being recorded: which step of the window's animation, which end, and what the step was.</summary>
        private struct Row
        {
            public int Step;
            public bool From;
            public string Fingerprint;
        }

        private static UIAnimationWindow window;
        private static UIAnimationPlayer player;
        private static readonly List<Row> rows = new List<Row>();

        // Set while Finish and Cancel end the preview themselves, so that is not taken for the preview
        // being ended from outside.
        private static bool closing;

        static UIAnimationRecording()
        {
            UIAnimationPreview.Ended += OnPreviewEnded;
        }

        /// <summary>The window recording right now, or null.</summary>
        public static UIAnimationWindow Window
        {
            get { return window; }
        }

        public static bool IsRecording(UIAnimationWindow candidate)
        {
            return window != null && window == candidate;
        }

        /// <summary>Whether this row is being recorded.</summary>
        public static bool IsArmed(SerializedProperty step, bool from)
        {
            if (window == null || UIAnimationWindow.Of(step.serializedObject) != window) return false;

            UIAnimation animation;
            UIAnimationStep data;
            int index;

            return window.TryGetStep(step, out animation, out data, out index) && IndexOf(index, from) >= 0;
        }

        /// <summary>Whether this row's record button can be pressed, and if not, why. Reads nothing.</summary>
        public static bool CanRecord(SerializedProperty step, bool from, out string reason)
        {
            UIAnimationWindow owner = UIAnimationWindow.Of(step.serializedObject);
            UIAnimation animation;
            UIAnimationStep data;
            int index;

            if (owner == null || !owner.TryGetStep(step, out animation, out data, out index))
            {
                reason = "This step is not part of the window's animation.";
                return false;
            }

            if (window != null && window != owner)
            {
                reason = "'" + window.titleContent.text + "' is recording in another window. Finish or Cancel it there first.";
                return false;
            }

            return CanRead(data, from, owner.Owner, out reason);
        }

        /// <summary>
        /// The record button: starts a session on its first press, adds this row to the one running, or takes
        /// it back out again.
        /// </summary>
        public static void Toggle(SerializedProperty step, bool from)
        {
            UIAnimationWindow owner = UIAnimationWindow.Of(step.serializedObject);
            UIAnimation animation;
            UIAnimationStep data;
            int index;

            if (owner == null || !owner.TryGetStep(step, out animation, out data, out index)) return;

            if (window == owner)
            {
                int armed = IndexOf(index, from);
                if (armed >= 0)
                {
                    rows.RemoveAt(armed);
                    owner.Repaint();
                    return;
                }
            }
            else if (window != null)
            {
                return;
            }

            UIAnimationPlayer scene = owner.Owner;

            string reason;
            if (!CanRead(data, from, scene, out reason)) return;

            if (window == null)
            {
                // A preview already running is joined where it stands, and refreshed so it covers this step.
                if (!UIAnimationPreview.Hold(owner, scene)) return;

                window = owner;
                player = scene;
                rows.Clear();
            }
            else
            {
                // A step or target added since the preview began was never captured at rest, so it would
                // have nothing to measure Baseline from and would not be put back. What a second Play
                // does first fixes both.
                UIAnimationPreview.Refresh(scene);
            }

            rows.Add(new Row { Step = index, From = from, Fingerprint = FingerprintOf(data) });

            // Listed in the order the steps run, From before To, however they were clicked.
            rows.Sort((a, b) => a.Step != b.Step ? a.Step.CompareTo(b.Step) : b.From.CompareTo(a.From));

            owner.Repaint();
            SceneView.RepaintAll();
        }

        /// <summary>Writes every row, then ends the preview, putting every object back. Closing the window does this.</summary>
        public static void Finish()
        {
            if (window == null) return;

            UIAnimationWindow owner = window;
            UIAnimationPlayer scene = player;
            var failures = new List<string>();

            SerializedObject serialized = owner.Serialized;
            UIAnimation animation = owner.Animation;

            if (serialized != null && serialized.targetObject != null && animation != null && scene != null)
            {
                serialized.Update();

                for (int i = 0; i < rows.Count; i++)
                {
                    Row row = rows[i];
                    string label = Describe(owner, row);

                    if (!StillThere(animation, row))
                    {
                        failures.Add(label + " changed while it was being recorded, so nothing was written to it.");
                        continue;
                    }

                    UIAnimationReading reading;
                    bool deferred;
                    string reason;

                    if (!TryRead(animation, row.Step, row.From, scene, true, out reading, out deferred, out reason))
                    {
                        failures.Add(label + ": " + reason);
                        continue;
                    }

                    SerializedProperty step = serialized.FindProperty(owner.StepPath(row.Step));
                    if (step != null) Write(step, row.From, reading);
                }

                serialized.ApplyModifiedProperties();
            }

            Close(owner, "Record Animation Values");

            for (int i = 0; i < failures.Count; i++)
            {
                UIAnimationLog.Warn("Recording", "A VALUE WAS NOT RECORDED.", failures[i], scene);
            }
        }

        /// <summary>Ends the preview, putting every object back, and writes nothing.</summary>
        public static void Cancel()
        {
            if (window == null) return;

            Close(window, "Cancel Recording");
        }

        private static void Close(UIAnimationWindow owner, string undoName)
        {
            int group = UIAnimationPreview.UndoGroup;

            closing = true;
            window = null;
            player = null;
            rows.Clear();

            try
            {
                UIAnimationPreview.Release(owner);
                UIAnimationPreview.End();
            }
            finally
            {
                closing = false;
            }

            if (group >= 0) Undo.CollapseUndoOperations(group);
            Undo.SetCurrentGroupName(undoName);

            if (owner != null) owner.Repaint();
            InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>
        /// The preview was ended from outside - play mode, a recompile, another player's preview starting.
        /// Everything is already back at rest, and with the preview gone there is nothing left to measure
        /// from, so the session is over and the pose is lost.
        /// </summary>
        private static void OnPreviewEnded()
        {
            if (closing || window == null) return;

            UIAnimationWindow owner = window;
            UIAnimationPlayer scene = player;
            int count = rows.Count;

            window = null;
            player = null;
            rows.Clear();

            if (count > 0)
            {
                UIAnimationLog.Warn("Recording", "IT WAS CANCELLED - NOTHING WAS WRITTEN.",
                    "The preview it was recording from ended under it: play mode, a recompile, its scene closing, or a " +
                    "preview started on another player. Every object is back where it was.", scene);
            }

            if (owner != null) owner.Repaint();
        }

        private static int IndexOf(int step, bool from)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Step == step && rows[i].From == from) return i;
            }

            return -1;
        }

        /// <summary>
        /// Whether a row still means what it meant when it was added. Rows are kept by step index, so a step
        /// moved, retyped or pointed elsewhere mid-session would otherwise be written with another step's value.
        /// </summary>
        private static bool StillThere(UIAnimation animation, Row row)
        {
            if (row.Step >= animation.Steps.Count) return false;

            UIAnimationStep step = animation.Steps[row.Step];
            if (step == null || FingerprintOf(step) != row.Fingerprint) return false;

            return !row.From || step.UseFrom;
        }

        private static string FingerprintOf(UIAnimationStep step)
        {
            Object slot = UIAnimationTargets.SlotOf(step, step.Type);

            return (int)step.Type + "|" + (slot != null ? slot.GetInstanceID() : 0) + "|" + step.TargetPath + "|" +
                   (step.Type == UIAnimationStepType.CustomProperty
                       ? step.PropertyComponent + "." + step.PropertyMember + ":" + (int)step.PropertyKind
                       : step.ShaderProperty);
        }

        private static string Describe(UIAnimationWindow owner, Row row)
        {
            return "'" + owner.AnimationName + "' step " + row.Step + " " + (row.From ? "From" : "To");
        }

        // ---------------------------------------------------------------- reading

        /// <summary>
        /// Whether a row could be recorded from this player's scene, judged without reading anything: a target
        /// to read, a Custom Property member that is there, a material that has the property.
        /// </summary>
        private static bool CanRead(UIAnimationStep step, bool from, UIAnimationPlayer owner, out string reason)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                reason = "Not in play mode - values changed there are thrown away when it ends.";
                return false;
            }

            if (owner == null)
            {
                reason = "A shared animation set has no scene to record from. Fill Preview On at the top of this " +
                         "window with a player that uses the set.";
                return false;
            }

            if (EditorUtility.IsPersistent(owner))
            {
                reason = "'" + owner.name + "' is in a prefab asset, not a scene. Open the prefab, or record from an " +
                         "instance of it in a scene.";
                return false;
            }

            if (from && step.FromMode == UIAnimationEndpointMode.Current)
            {
                reason = "This From is set to Current, which ignores the value here.";
                return false;
            }

            string problem;
            Object target = UIAnimationTargets.Resolve(step.Type, UIAnimationTargets.SlotOf(step, step.Type), step.TargetPath,
                owner.gameObject, out problem);

            if (target == null)
            {
                reason = problem ?? "This step has nothing to record a value from.";
                return false;
            }

            if (step.Type == UIAnimationStepType.CustomProperty)
            {
                Component component;
                MemberInfo member;

                if (!UIAnimationProperties.TryLocate(target as GameObject, step.PropertyComponent, step.PropertyMember,
                        step.PropertyKind, out component, out member, out reason))
                {
                    return false;
                }
            }
            else if (UIAnimationStep.TargetKindOf(step.Type) == UIAnimationTargetKind.Material)
            {
                Material material = UIAnimationTargets.SourceMaterialOf(target as UIMaterialInstance);

                if (material == null)
                {
                    reason = "'" + target.name + "' has no material to record from.";
                    return false;
                }

                if (string.IsNullOrEmpty(step.ShaderProperty) || !material.HasProperty(step.ShaderProperty))
                {
                    reason = "Shader '" + material.shader.name + "' has no property '" + step.ShaderProperty + "'.";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// What recording this row would store, from what the scene holds now. allowGetters is false on every
        /// repaint: a Custom Property is then left unread, and deferred says it is read on Finish.
        /// </summary>
        private static bool TryRead(UIAnimation animation, int stepIndex, bool from, UIAnimationPlayer owner, bool allowGetters,
            out UIAnimationReading reading, out bool deferred, out string reason)
        {
            reading = default(UIAnimationReading);
            deferred = false;

            UIAnimationStep step = animation.Steps[stepIndex];
            if (!CanRead(step, from, owner, out reason)) return false;

            bool custom = step.Type == UIAnimationStepType.CustomProperty;
            reading.Kind = custom ? UIAnimationStep.ValueKindOf(step.PropertyKind) : UIAnimationStep.ValueKindOf(step.Type);
            reading.TwoDimensional = custom
                ? step.PropertyKind == UIAnimationPropertyKind.Vector2
                : UIAnimationStep.IsTwoDimensional(step.Type);

            string problem;
            Object target = UIAnimationTargets.Resolve(step.Type, UIAnimationTargets.SlotOf(step, step.Type), step.TargetPath,
                owner.gameObject, out problem);

            Vector4 live;
            string liveText = null;

            if (custom)
            {
                if (!allowGetters)
                {
                    deferred = true;
                    return true;
                }

                if (!ReadMember(step, target as GameObject, out live, out liveText, out reason)) return false;
            }
            else if (!ReadLive(step, target, out live))
            {
                reason = "This step has nothing to record a value from.";
                return false;
            }

            UIAnimationEndpointMode mode = from ? step.FromMode : step.ToMode;

            // A Current To is only an offset from where the step starts when there is no From. With one, the
            // row offers Absolute and Baseline only, and shows - and stores - a Current as Baseline.
            if (!from && mode == UIAnimationEndpointMode.Current && step.UseFrom) mode = UIAnimationEndpointMode.Baseline;

            bool rotation = step.Type == UIAnimationStepType.Rotation;
            GameObject scene = owner.gameObject;

            switch (mode)
            {
                case UIAnimationEndpointMode.Absolute:
                    reading.Number = rotation ? WrapAngles(live) : live;
                    reading.Text = liveText;
                    break;

                case UIAnimationEndpointMode.Baseline:
                {
                    Vector4 rest;
                    string restText;

                    if (!UIAnimationSimulation.TryRestOf(step, scene, out rest, out restText))
                    {
                        reason = "Its resting value is not known, so there is nothing to measure Baseline from.";
                        return false;
                    }

                    if (reading.Kind == UIAnimationValueKind.Text)
                    {
                        // Baseline text is the resting text with this added onto the end.
                        string head = restText ?? string.Empty;
                        string now = liveText ?? string.Empty;

                        if (!now.StartsWith(head, System.StringComparison.Ordinal))
                        {
                            reason = "Baseline adds text onto the end of the resting text (\"" + Shorten(head) + "\"), and the " +
                                     "text no longer starts with it. Switch to Absolute to record the text as it is.";
                            return false;
                        }

                        reading.Text = now.Substring(head.Length);
                    }
                    else
                    {
                        reading.Number = live - rest;
                        if (rotation) reading.Number = WrapAngles(reading.Number);
                    }

                    break;
                }

                default:
                {
                    Vector4 start;

                    if (reading.Kind == UIAnimationValueKind.Text
                        || !UIAnimationSimulation.TryStartOf(animation, scene, stepIndex, out start))
                    {
                        reason = "Where this step starts cannot be worked out, so there is nothing to measure Current from.";
                        return false;
                    }

                    reading.Number = live - start;
                    if (rotation) reading.Number = WrapAngles(reading.Number);
                    break;
                }
            }

            if (reading.Kind == UIAnimationValueKind.Int) reading.Number.x = Mathf.Round(reading.Number.x);

            // The same rule as dragging an endpoint in the Scene view: a snapping step stores whole numbers,
            // halves rounded away from zero. Z is left alone, as it is never snapped.
            if (reading.Kind == UIAnimationValueKind.Vector && UIAnimationStep.SupportsSnapping(step.Type)
                && (step.Snapping || animation.SnapsEveryStep))
            {
                reading.Number.x = (float)System.Math.Round(reading.Number.x, System.MidpointRounding.AwayFromZero);
                reading.Number.y = (float)System.Math.Round(reading.Number.y, System.MidpointRounding.AwayFromZero);
            }

            reason = null;
            return true;
        }

        /// <summary>The value the target holds right now, in the step's own terms. Never a Custom Property.</summary>
        private static bool ReadLive(UIAnimationStep step, Object target, out Vector4 value)
        {
            value = Vector4.zero;
            UIAnimationStepType type = step.Type;

            var rect = target as RectTransform;
            if (rect != null && UIAnimationRectState.Covers(type))
            {
                value = UIAnimationRectState.Of(rect).Read(type);
                return true;
            }

            var group = target as CanvasGroup;
            if (group != null && type == UIAnimationStepType.CanvasGroupAlpha)
            {
                value = new Vector4(group.alpha, 0f, 0f, 0f);
                return true;
            }

            var graphic = target as UnityEngine.UI.Graphic;
            if (graphic != null && type == UIAnimationStepType.GraphicColor)
            {
                value = graphic.color;
                return true;
            }

            if (graphic != null && type == UIAnimationStepType.GraphicAlpha)
            {
                value = new Vector4(graphic.color.a, 0f, 0f, 0f);
                return true;
            }

            // During the session the preview has given the Graphic its instance, so this is what was posed.
            var instance = target as UIMaterialInstance;
            if (instance != null)
            {
                Material material = UIAnimationTargets.SourceMaterialOf(instance);
                if (material == null || string.IsNullOrEmpty(step.ShaderProperty) || !material.HasProperty(step.ShaderProperty)) return false;

                value = type == UIAnimationStepType.MaterialColor
                    ? (Vector4)material.GetColor(step.ShaderProperty)
                    : new Vector4(material.GetFloat(step.ShaderProperty), 0f, 0f, 0f);
                return true;
            }

            return false;
        }

        /// <summary>
        /// A Custom Property's value, read through its getter - only ever on a click. A counted number is the
        /// number its text shows, with the text alongside.
        /// </summary>
        private static bool ReadMember(UIAnimationStep step, GameObject host, out Vector4 number, out string text, out string reason)
        {
            number = Vector4.zero;
            text = null;

            Component component;
            MemberInfo member;

            if (!UIAnimationProperties.TryLocate(host, step.PropertyComponent, step.PropertyMember, step.PropertyKind,
                    out component, out member, out reason))
            {
                return false;
            }

            object read;

            try
            {
                read = UIAnimationProperties.Read(component, member);
            }
            catch (System.Exception exception)
            {
                // Reflection wraps whatever the getter threw.
                System.Exception cause = exception.InnerException ?? exception;
                reason = "Reading " + UIAnimationProperties.Describe(component.GetType().FullName, member.Name, step.PropertyKind) +
                         " threw " + cause.GetType().Name + " (" + cause.Message + ").";
                return false;
            }

            switch (step.PropertyKind)
            {
                case UIAnimationPropertyKind.Float:
                    number.x = (float)read;
                    break;

                case UIAnimationPropertyKind.Int:
                    number.x = (int)read;
                    break;

                case UIAnimationPropertyKind.Vector2:
                {
                    var read2 = (Vector2)read;
                    number = new Vector4(read2.x, read2.y, 0f, 0f);
                    break;
                }

                case UIAnimationPropertyKind.Vector3:
                    number = (Vector3)read;
                    break;

                case UIAnimationPropertyKind.Color:
                    number = (Color)read;
                    break;

                case UIAnimationPropertyKind.Text:
                    text = (string)read ?? string.Empty;
                    break;

                default:
                    text = (string)read ?? string.Empty;
                    number.x = UIAnimationProperties.ParseNumber(text);
                    break;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// Rotation reads back as 0 to 360, so -10 would come out as 350 - and a Rotation step turns the long way
        /// round to 350. Nearest to zero is what was almost always meant.
        /// </summary>
        private static Vector4 WrapAngles(Vector4 angles)
        {
            return new Vector4(Mathf.DeltaAngle(0f, angles.x), Mathf.DeltaAngle(0f, angles.y), Mathf.DeltaAngle(0f, angles.z), 0f);
        }

        private static string Shorten(string text)
        {
            return text.Length <= 24 ? text : text.Substring(0, 24) + "…";
        }

        /// <summary>A reading into the row's own field. A Z the row never shows is left as it was rather than zeroed.</summary>
        private static void Write(SerializedProperty step, bool from, UIAnimationReading reading)
        {
            string prefix = from ? "From" : "To";

            switch (reading.Kind)
            {
                case UIAnimationValueKind.Vector:
                {
                    SerializedProperty value = step.FindPropertyRelative(prefix + "Vector");
                    Vector3 current = value.vector3Value;

                    value.vector3Value = reading.TwoDimensional
                        ? new Vector3(reading.Number.x, reading.Number.y, current.z)
                        : (Vector3)reading.Number;
                    break;
                }

                case UIAnimationValueKind.Color:
                    step.FindPropertyRelative(prefix + "Color").colorValue = reading.Number;
                    break;

                case UIAnimationValueKind.Text:
                    step.FindPropertyRelative(prefix + "Text").stringValue = reading.Text ?? string.Empty;
                    break;

                // An int member's value is kept in the float field.
                default:
                    step.FindPropertyRelative(prefix + "Float").floatValue = reading.Number.x;
                    break;
            }
        }

        // ---------------------------------------------------------------- drawing

        private static readonly Color ArmedTint = new Color(0.95f, 0.25f, 0.2f, 0.16f);

        public const string ButtonTooltip =
            "Record: copies this value from the scene.\n\n" +
            "Click it, then pose the object in the Scene view - move, resize or recolour it, or change it in its " +
            "Inspector - and press Finish at the top of this window. Nothing moves when it starts, the row shows " +
            "the value it will get while you pose, and Finish puts every object back afterwards. Click more " +
            "rows to record several at once; click a recording row again to stop recording it.";

        private const string DeferredTooltip =
            "A Custom Property is read through its getter, which is code - so it is read once, when you press " +
            "Finish, rather than every time the window redraws.";

        public static Texture RecordIcon
        {
            get { return EditorGUIUtility.FindTexture(EditorGUIUtility.isProSkin ? "d_Animation.Record" : "Animation.Record"); }
        }

        private static GUIStyle wrapStyle;

        private static GUIStyle WrapStyle
        {
            get
            {
                if (wrapStyle == null) wrapStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                return wrapStyle;
            }
        }

        /// <summary>A recording row's value field: what Finish will write, read-only and live.</summary>
        public static void DrawArmedValue(Rect rect, SerializedProperty step, bool from)
        {
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(rect.x - 2f, rect.y - 1f, rect.width + 4f, rect.height + 2f), ArmedTint);
            }

            UIAnimation animation;
            UIAnimationStep data;
            int index;

            if (window == null || player == null || !window.TryGetStep(step, out animation, out data, out index)) return;

            DrawLive(rect, animation, index, from);
        }

        private static void DrawLive(Rect rect, UIAnimation animation, int index, bool from)
        {
            UIAnimationReading reading;
            bool deferred;
            string reason;

            if (!TryRead(animation, index, from, player, false, out reading, out deferred, out reason))
            {
                GUI.Label(rect, new GUIContent(reason, reason), EditorStyles.miniLabel);
                return;
            }

            if (deferred)
            {
                GUI.Label(rect, new GUIContent("read on Finish", DeferredTooltip), EditorStyles.miniLabel);
                return;
            }

            DrawReading(rect, reading);
        }

        private static void DrawReading(Rect rect, UIAnimationReading reading)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                switch (reading.Kind)
                {
                    case UIAnimationValueKind.Vector:
                        if (reading.TwoDimensional) EditorGUI.Vector2Field(rect, GUIContent.none, reading.Number);
                        else EditorGUI.Vector3Field(rect, GUIContent.none, reading.Number);
                        break;

                    case UIAnimationValueKind.Color:
                        EditorGUI.ColorField(rect, GUIContent.none, reading.Number, false, true, false);
                        break;

                    case UIAnimationValueKind.Int:
                        EditorGUI.IntField(rect, Mathf.RoundToInt(reading.Number.x));
                        break;

                    case UIAnimationValueKind.Text:
                        EditorGUI.TextField(rect, reading.Text ?? string.Empty);
                        break;

                    default:
                        EditorGUI.FloatField(rect, reading.Number.x);
                        break;
                }
            }
        }

        /// <summary>
        /// The box at the top of a pop-up animation window: what is being recorded, with Finish and Cancel, or
        /// - when nothing is - one line saying how to start.
        /// </summary>
        public static void DrawPanel(UIAnimationWindow owner)
        {
            if (window == null)
            {
                EditorGUILayout.LabelField(new GUIContent(
                    " Record on a From or To row copies it from the scene: click it, pose the object, then Finish.",
                    RecordIcon, ButtonTooltip), WrapStyle);
                return;
            }

            if (window != owner)
            {
                EditorGUILayout.HelpBox("'" + window.titleContent.text + "' is recording in another window. Finish or " +
                                        "Cancel it there to record here.", MessageType.Info);
                return;
            }

            // Deleted from under the recording: nothing is left to read or to put back.
            if (player == null)
            {
                Cancel();
                GUIUtility.ExitGUI();
            }

            // Rows whose step was moved, retyped or pointed elsewhere since are dropped, rather than kept to be
            // written into whatever step is there now.
            UIAnimation animation = owner.Animation;

            for (int i = rows.Count - 1; i >= 0; i--)
            {
                if (animation == null || !StillThere(animation, rows[i])) rows.RemoveAt(i);
            }

            Rect box = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(box, ArmedTint);

            EditorGUILayout.LabelField(new GUIContent(" Recording " + (rows.Count == 1 ? "1 value" : rows.Count + " values"),
                RecordIcon), EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Pose the objects in the Scene view (or in their Inspector), then press Finish. Nothing is written " +
                "until then, and Finish and Cancel both put every object back. Click record on more rows to add them.",
                WrapStyle);

            if (animation != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    Row row = rows[i];
                    Rect line = EditorGUILayout.GetControlRect();

                    UIAnimationStep step = animation.Steps[row.Step];
                    UIAnimationEndpointMode mode = row.From ? step.FromMode : step.ToMode;
                    if (!row.From && mode == UIAnimationEndpointMode.Current && step.UseFrom) mode = UIAnimationEndpointMode.Baseline;

                    var labelRect = new Rect(line.x, line.y, Mathf.Min(150f, line.width * 0.42f), line.height);
                    var removeRect = new Rect(line.xMax - 20f, line.y, 20f, line.height);
                    var valueRect = new Rect(labelRect.xMax + 4f, line.y, Mathf.Max(30f, removeRect.x - 4f - labelRect.xMax - 4f), line.height);

                    EditorGUI.LabelField(labelRect, "Step " + row.Step + " " + (row.From ? "From" : "To") + " (" + mode + ")");
                    DrawLive(valueRect, animation, row.Step, row.From);

                    if (GUI.Button(removeRect, new GUIContent("×", "Stop recording this row."), EditorStyles.miniButton))
                    {
                        rows.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Cancel", "Puts every object back and writes nothing."), GUILayout.Width(80f)))
            {
                Cancel();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button(new GUIContent("Finish", "Writes the values shown, then puts every object back. One " +
                    "Undo step undoes the lot."), GUILayout.Width(80f)))
            {
                Finish();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
