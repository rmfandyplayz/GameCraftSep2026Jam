using rmf_claude.DOTweenUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hand-test fixture for the DOTween UI player's SetTo / SetFrom / ClearOverrides (UITester2.unity,
/// station N2). Play mode.
///
/// Three animations, one Custom Property step each: Level (the float property below, drawn as the
/// bar), Coins (the int property below, which has its From ticked) and Message (the text's own
/// .text, typed out). Each button sets a step's To - and for Coins its From - from code, then plays
/// it. The Inspector values are 100, 0 to 50 and "Authored in the Inspector.", which Clear goes back
/// to. The white line shows what the calls returned, the yellow line what should happen.
///
/// The coin total is kept apart from the number on screen, the way a game would keep it. Playing
/// an animation that is already running stops it where it stands, so a click mid-count sees the
/// half-counted number on screen; counting from there to the real total is what makes every click
/// add 7.
/// </summary>
public class SetToFromTest : MonoBehaviour
{
    private static readonly string[] Messages =
    {
        "Set from code.",
        "A second message, a little longer than the first.",
        "<b>Rich text</b> tags are never shown half typed.",
        "Short.",
    };

    [SerializeField] private UIAnimationPlayer player;
    [SerializeField] private RectTransform levelFill;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private TMP_Text coinsLabel;
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text expect;
    [SerializeField] private float level = 60f;
    [SerializeField] private int coins = 12;

    [Header("Buttons")]
    [SerializeField] private Button levelDown;
    [SerializeField] private Button levelUp;
    [SerializeField] private Button addCoins;
    [SerializeField] private Button nextMessage;
    [SerializeField] private Button clear;
    [SerializeField] private Button refused;

    private float levelTarget;
    private int coinTotal;
    private int messageIndex;

    public float Level
    {
        get { return level; }
        set { level = Mathf.Clamp(value, 0f, 100f); Redraw(); }
    }

    public int Coins
    {
        get { return coins; }
        set { coins = Mathf.Max(0, value); Redraw(); }
    }

    private void Awake()
    {
        levelTarget = level;
        coinTotal = coins;

        levelDown.onClick.AddListener(() => ChangeLevel(-25f));
        levelUp.onClick.AddListener(() => ChangeLevel(25f));
        addCoins.onClick.AddListener(AddCoins);
        nextMessage.onClick.AddListener(NextMessage);
        clear.onClick.AddListener(Clear);
        refused.onClick.AddListener(Refused);
    }

    private void ChangeLevel(float delta)
    {
        levelTarget = Mathf.Clamp(levelTarget + delta, 0f, 100f);

        bool ok = player.SetTo("Level", 0, levelTarget);
        player.Play("Level");

        Show("SetTo(\"Level\", 0, " + levelTarget + ") returned " + ok,
            "the bar slides from where it is to " + levelTarget + ", not to the Inspector's 100.");
    }

    private void AddCoins()
    {
        int from = coins;
        coinTotal += 7;

        bool okFrom = player.SetFrom("Coins", 0, (float)from);
        bool okTo = player.SetTo("Coins", 0, (float)coinTotal);
        player.Play("Coins");

        Show("SetFrom(\"Coins\", 0, " + from + ") returned " + okFrom + ", SetTo(..., " + coinTotal + ") returned " + okTo,
            "counts " + from + " to " + coinTotal + " in whole numbers. Click again mid-count: it carries on from where it has got to, and still lands 7 higher per click.");
    }

    private void NextMessage()
    {
        string text = Messages[messageIndex];
        messageIndex = (messageIndex + 1) % Messages.Length;

        bool ok = player.SetTo("Message", 0, text);
        player.Play("Message");

        Show("SetTo(\"Message\", 0, \"" + text + "\") returned " + ok,
            "the message is typed over the old one.");
    }

    private void Clear()
    {
        player.ClearOverrides("Level");
        player.ClearOverrides("Coins");
        player.ClearOverrides("Message");
        levelTarget = 100f;
        coinTotal = 50;

        player.Play("Level");
        player.Play("Coins");
        player.Play("Message");

        Show("ClearOverrides on all three, then played them",
            "the Inspector values: the bar goes to 100, coins count 0 to 50, \"Authored in the Inspector.\"");
    }

    private void Refused()
    {
        bool wrongKind = player.SetTo("Level", 0, Color.red);
        bool fromOff = player.SetFrom("Level", 0, 10f);
        bool noStep = player.SetTo("Level", 5, 10f);

        Show("wrong kind returned " + wrongKind + ", From off returned " + fromOff + ", no step 5 returned " + noStep,
            "all three False, nothing moves, and three warnings in the Console saying why.");
    }

    private void Show(string happened, string expected)
    {
        status.text = happened;
        expect.text = "Expect: " + expected;
    }

    private void Redraw()
    {
        if (levelFill != null) levelFill.anchorMax = new Vector2(level / 100f, 1f);
        if (levelLabel != null) levelLabel.text = "Level " + Mathf.RoundToInt(level);
        if (coinsLabel != null) coinsLabel.text = coins + " coins";
    }
}
