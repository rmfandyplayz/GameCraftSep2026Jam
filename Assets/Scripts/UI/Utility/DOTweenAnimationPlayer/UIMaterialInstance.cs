// -----------------------------------------------------------------------------
// UI Animation Utility
//
// AI-GENERATED. Authored by Claude (Anthropic) via Claude Code, September 2026,
// to a written design brief by the project author.
// See the README.md beside this file for usage.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace rmf_claude.DOTweenUI
{

    /// <summary>
    /// Gives a UI Graphic its own material instance so shader properties can be animated
    /// without ever writing to the shared project asset.
    ///
    /// Add this to any Image / RawImage / TMP text whose material a UIAnimationPlayer should drive.
    /// MaterialFloat and MaterialColor steps target this component, not a Graphic or Material,
    /// so there is no inspector slot that could point a tween at a shared asset by accident.
    ///
    /// Note: MaterialPropertyBlock does not work with UGUI - CanvasRenderer ignores it - so a real
    /// material instance is the only option.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class UIMaterialInstance : MonoBehaviour
    {
        private Graphic graphic;
        private Material instance;
        private bool ownsInstance;
        private bool initialized;

    #if UNITY_EDITOR
        // What the Graphic's own material field held before the instance went in, for EditorRelease.
        private Material editorOriginal;
        private string editorField;
    #endif

        /// <summary>The per-element material. Safe to write to.</summary>
        public Material Material
        {
            get
            {
                Initialize();
                return instance;
            }
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            // Only destroy what we created. TMP owns its fontMaterial.
            if (ownsInstance && instance != null) Destroy(instance);
            instance = null;
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;

            graphic = GetComponent<Graphic>();
            if (graphic == null) return;

        #if UNITY_EDITOR
            // Noted for the preview, which puts it back. The serialized field, not the getter: an Image with
            // no material of its own reads back as the default UI material, and writing that back would give
            // it one.
            if (!Application.isPlaying)
            {
                editorField = graphic is TMP_Text ? "m_sharedMaterial" : "m_Material";
                var serialized = new UnityEditor.SerializedObject(graphic);
                UnityEditor.SerializedProperty field = serialized.FindProperty(editorField);
                editorOriginal = field != null ? field.objectReferenceValue as Material : null;
            }
        #endif

            // TextMeshProUGUI overrides materialForRendering to read m_sharedMaterial and ignores
            // Graphic.material entirely, so assigning a clone there would have no visible effect.
            // fontMaterial is TMP own instance, and TMP owns its lifetime - do not destroy it.
            TMP_Text text = graphic as TMP_Text;
            if (text != null)
            {
                instance = text.fontMaterial;
                ownsInstance = false;
            }
            else
            {
                Material source = graphic.material;
                if (source == null) return;

                instance = new Material(source);
                instance.name = source.name + " (Instance)";
                ownsInstance = true;
                graphic.material = instance;
            }

            WarnIfUnderStencilMask();
        }

    #if UNITY_EDITOR
        /// <summary>
        /// Gives the Graphic back the material it had before the instance, and forgets the instance - for the
        /// end of the edit-mode preview, the only thing that instances one out of play mode. There the
        /// instance is assigned to a scene object like any edit, and a scene saved after a material preview
        /// kept a copy of it inside the scene file, cut off from the material asset (measured). The next
        /// read of Material makes a new instance.
        /// </summary>
        public void EditorRelease()
        {
            if (!initialized) return;

            if (graphic != null && editorField != null)
            {
                var serialized = new UnityEditor.SerializedObject(graphic);
                UnityEditor.SerializedProperty field = serialized.FindProperty(editorField);

                if (field != null)
                {
                    field.objectReferenceValue = editorOriginal;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            if (ownsInstance && instance != null) DestroyImmediate(instance);

            instance = null;
            ownsInstance = false;
            editorOriginal = null;
            editorField = null;
            initialized = false;
        }
    #endif

        /// <summary>
        /// A stencil Mask makes UGUI take a one-time cached copy of this material and never re-sync it,
        /// so animated properties would never reach the screen. RectMask2D does not do this.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void WarnIfUnderStencilMask()
        {
            MaskableGraphic maskable = graphic as MaskableGraphic;
            if (maskable == null || !maskable.maskable) return;

            Transform current = transform.parent;
            while (current != null)
            {
                Mask mask = current.GetComponent<Mask>();
                if (mask != null && mask.enabled)
                {
                    UIAnimationLog.Warn("UIMaterialInstance on '" + name + "'", "ANIMATED SHADER PROPERTIES WON'T SHOW.",
                        "It is under a stencil Mask ('" + mask.name + "'), and UGUI caches a copy of the material " +
                        "for stencil rendering. Use a RectMask2D instead, or untick Maskable on this Graphic.",
                        this);
                    return;
                }

                current = current.parent;
            }
        }
    }
}
