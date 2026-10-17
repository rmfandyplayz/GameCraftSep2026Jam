using rmf_claude.DOTweenUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hand-test fixture for punch and shake being put back when cut short (UITester.unity, station P2).
/// Play mode.
///
/// Punch and Shake play on the box; press them as fast as you like. Each press restarts the animation,
/// which stops the running one - and a punch or shake stopped part way goes back to where it started,
/// so the box always settles on its resting scale, angle and place. Before September 2026 it kept
/// wherever the wobble had got to and grew with every press.
/// </summary>
public class PunchSpamTest : MonoBehaviour
{
    [SerializeField] private UIAnimationPlayer player;
    [SerializeField] private RectTransform box;
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text expect;

    [Header("Buttons")]
    [SerializeField] private Button punch;
    [SerializeField] private Button shake;
    [SerializeField] private Button stopAll;

    private Vector2 rest;

    private void Awake()
    {
        rest = box.anchoredPosition;

        punch.onClick.AddListener(() => Press("Punch",
            "a quick bounce. Spam it: once it settles, scale reads 1.00 every time."));
        shake.onClick.AddListener(() => Press("Shake",
            "a rattle. Spam it: once it settles, angle reads 0.0 and place (0, 0) every time."));
        stopAll.onClick.AddListener(() =>
        {
            player.StopAll();
            expect.text = "Expect: stops at once, and snaps straight back to scale 1.00, angle 0.0, place (0, 0).";
        });
    }

    private void Press(string animationName, string expected)
    {
        player.Play(animationName);
        expect.text = "Expect: " + expected;
    }

    private void Update()
    {
        float angle = Mathf.DeltaAngle(0f, box.localEulerAngles.z);
        Vector2 moved = box.anchoredPosition - rest;

        status.text = "scale " + box.localScale.x.ToString("0.00")
            + "     angle " + angle.ToString("0.0")
            + "     place (" + Mathf.RoundToInt(moved.x) + ", " + Mathf.RoundToInt(moved.y) + ") from rest";
    }
}
