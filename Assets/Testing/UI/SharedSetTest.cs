using rmf_claude.DOTweenUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hand-test fixture for Shared Animations and each player's own On Complete (UITester.unity, station
/// P1). Play mode.
///
/// S1 and S2 both use "UITester Shared Set". Each has its own On Complete on the set's Slide, wired to
/// SlideDone with its own name, so the same animation counts on a different line depending on who
/// played it. S1 also has a Fade of its own, which overrides the set's; S2 plays the set's. The white
/// line shows what happened, the yellow line what should have.
/// </summary>
public class SharedSetTest : MonoBehaviour
{
    [SerializeField] private UIAnimationPlayer s1;
    [SerializeField] private UIAnimationPlayer s2;
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text expect;

    [Header("Buttons")]
    [SerializeField] private Button slideS1;
    [SerializeField] private Button slideS2;
    [SerializeField] private Button fadeS1;
    [SerializeField] private Button fadeS2;
    [SerializeField] private Button spinBoth;

    private int s1Count;
    private int s2Count;

    private void Awake()
    {
        slideS1.onClick.AddListener(() => Play(s1, "Slide",
            "S1's box slides right and back. Then the S1 count goes up by 1, S2's does not."));
        slideS2.onClick.AddListener(() => Play(s2, "Slide",
            "S2's box slides right and back. Then the S2 count goes up by 1, S1's does not."));
        fadeS1.onClick.AddListener(() => Play(s1, "Fade",
            "S1 plays its OWN Fade: the box flashes red, it does not fade out."));
        fadeS2.onClick.AddListener(() => Play(s2, "Fade",
            "S2 plays the set's Fade - but S2's box has no Image of its own, so nothing fades, and the Console says A STEP WAS SKIPPED."));
        spinBoth.onClick.AddListener(() =>
        {
            s1.Play("Spin");
            s2.Play("Spin");
            expect.text = "Expect: both boxes spin once. Neither count changes: Spin has no On Complete on either player.";
        });
    }

    /// <summary>Wired to each player's own On Complete for the set's Slide, with the player's name.</summary>
    public void SlideDone(string who)
    {
        if (who == "S1") s1Count++;
        else if (who == "S2") s2Count++;
    }

    private void Play(UIAnimationPlayer player, string animationName, string expected)
    {
        player.Play(animationName);
        expect.text = "Expect: " + expected;
    }

    private void Update()
    {
        status.text = "Slide On Complete - S1: " + s1Count + "     S2: " + s2Count;
    }
}
