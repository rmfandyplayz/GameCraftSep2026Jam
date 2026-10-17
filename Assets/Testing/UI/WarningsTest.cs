using rmf_claude.DOTweenUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hand-test fixture for how the DOTween UI player's Console warnings read (UITester.unity, station W1).
/// Play mode.
///
/// Each button does something the player warns about. Every warning starts with who and what is going to
/// happen, in bold capitals, then the details. Problems with steps are found once, when the player wakes,
/// so those two are already in the Console as play mode starts, and pressing their buttons adds nothing.
/// </summary>
public class WarningsTest : MonoBehaviour
{
    [SerializeField] private UIAnimationPlayer player;
    [SerializeField] private TMP_Text expect;

    [Header("Buttons")]
    [SerializeField] private Button brokenPath;
    [SerializeField] private Button noClip;
    [SerializeField] private Button unknownName;
    [SerializeField] private Button badSetTo;

    private void Awake()
    {
        brokenPath.onClick.AddListener(() => Press(() => player.Play("Broken Path"),
            "nothing new. \"A STEP WILL BE SKIPPED.\" for Animation 'Broken Path' step 0 (Target Path 'Nothing/Here') was printed as play mode started - a step is checked once, when the player wakes."));
        noClip.onClick.AddListener(() => Press(() => player.Play("No Clip"),
            "nothing new, and no sound. \"A STEP WILL BE SKIPPED.\" for Animation 'No Clip' step 0 (no Clip) was printed as play mode started."));
        unknownName.onClick.AddListener(() => Press(() => player.Play("Nope"),
            "\"NOTHING HAPPENED - NO SUCH ANIMATION.\" - no animation named 'Nope'."));
        badSetTo.onClick.AddListener(() => Press(() => player.SetTo("Broken Path", 9, 1f),
            "\"SETTO WAS IGNORED.\" - it asked for step 9 of 'Broken Path', which has 1."));
    }

    private void Press(System.Action action, string expected)
    {
        expect.text = "Expect in the Console: " + expected;
        action();
    }
}
