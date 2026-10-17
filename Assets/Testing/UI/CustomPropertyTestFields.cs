using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Hand-test fixture for the DOTween UI player's Custom Property step (UITester.unity, station S2).
/// Everything tweenable here is a plain public FIELD, which has no setter to react with, so the
/// values are copied onto the object every frame - every editor tick too, so the edit-mode preview
/// shows them moving. Only a changed value is written, so an idle scene is never touched.
/// </summary>
[ExecuteAlways]
public class CustomPropertyTestFields : MonoBehaviour
{
    public Vector3 Angles;
    public Color Tint = Color.white;
    public string Caption = string.Empty;

    [SerializeField] private Image image;
    [SerializeField] private TMP_Text caption;

    private Vector3 appliedAngles;
    private Color appliedTint;
    private string appliedCaption;

    private void OnEnable()
    {
        // The scene is saved with the fields and the object agreeing, so there is nothing to write yet.
        appliedAngles = Angles;
        appliedTint = Tint;
        appliedCaption = Caption;

    #if UNITY_EDITOR
        if (!Application.isPlaying) EditorApplication.update += Apply;
    #endif
    }

    private void OnDisable()
    {
    #if UNITY_EDITOR
        EditorApplication.update -= Apply;
    #endif
    }

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (this == null) return;

        if (Angles != appliedAngles)
        {
            transform.localEulerAngles = Angles;
            appliedAngles = Angles;
        }

        if (image != null && Tint != appliedTint)
        {
            image.color = Tint;
            appliedTint = Tint;
        }

        if (caption != null && Caption != appliedCaption)
        {
            caption.text = Caption;
            appliedCaption = Caption;
        }
    }
}
