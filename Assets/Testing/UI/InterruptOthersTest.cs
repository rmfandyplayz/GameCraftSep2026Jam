using rmf_claude.DOTweenUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hand-test fixture for the DOTween UI player's Interrupt Others buttons and Stop(complete)
/// (UITester2.unity, station N1). Play mode only: the edit-mode preview never reaches the player's
/// interrupt code.
///
/// Travel moves the runner across, then shows DONE with a jingle, and its On Complete (wired in the
/// Inspector) counts here. Travel Endless swings back and forth forever. The three interrupt buttons
/// each play a small punch on the runner, set to STOP, COMPLETE + REPORT or COMPLETE + SILENT beside
/// its Interrupt Others. The white line shows what happened, the yellow line what should have.
/// </summary>
public class InterruptOthersTest : MonoBehaviour
{
    private const string Travel = "Travel";
    private const string TravelEndless = "Travel Endless";

    [SerializeField] private UIAnimationPlayer player;
    [SerializeField] private RectTransform runner;
    [SerializeField] private GameObject done;
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text expect;

    [Header("Buttons")]
    [SerializeField] private Button playTravel;
    [SerializeField] private Button playEndless;
    [SerializeField] private Button stop;
    [SerializeField] private Button stopComplete;
    [SerializeField] private Button stopAllComplete;
    [SerializeField] private Button interruptStop;
    [SerializeField] private Button interruptReport;
    [SerializeField] private Button interruptSilent;

    private string current;
    private string ended = "-";
    private int plays;
    private int onCompleteCount;
    private int onCompleteAtPress;

    private void Awake()
    {
        playTravel.onClick.AddListener(() => Begin(Travel));
        playEndless.onClick.AddListener(() => Begin(TravelEndless));

        stop.onClick.AddListener(() => EndCurrent(running => player.Stop(running),
            "Stops where it is. No DONE, no jingle. On Complete +0, ends Stopped.",
            "Stops where it is. On Complete +0, ends Stopped."));
        stopComplete.onClick.AddListener(() => EndCurrent(running => player.Stop(running, true),
            "Jumps to the right end and DONE shows, with NO jingle. On Complete +1, ends Completed.",
            "An endless loop has no end: stops where it is. On Complete +0, ends Stopped."));
        stopAllComplete.onClick.AddListener(() => EndCurrent(running => player.StopAll(true),
            "Jumps to the right end and DONE shows, with NO jingle. On Complete +1, ends Completed.",
            "An endless loop has no end: stops where it is. On Complete +0, ends Stopped."));

        interruptStop.onClick.AddListener(() => Interrupt("Interrupt STOP",
            "Stops where it is, then the runner punches. No DONE. On Complete +0, ends Interrupted.",
            "Stops where it is, then the runner punches. On Complete +0, ends Interrupted."));
        interruptReport.onClick.AddListener(() => Interrupt("Interrupt COMPLETE REPORT",
            "Jumps to the right end, DONE shows, NO jingle, then the runner punches. On Complete +1, ends Completed.",
            "An endless loop has no end: stops where it is, then punches. On Complete +0, ends Interrupted."));
        interruptSilent.onClick.AddListener(() => Interrupt("Interrupt COMPLETE SILENT",
            "Jumps to the right end, DONE shows, NO jingle, then the runner punches. On Complete +0, ends Interrupted.",
            "An endless loop has no end: stops where it is, then punches. On Complete +0, ends Interrupted."));
    }

    /// <summary>Wired to Travel's On Complete in the Inspector.</summary>
    public void CountOnComplete()
    {
        onCompleteCount++;
    }

    private void Begin(string animationName)
    {
        // Playing an animation that is already running always stops it where it stands, whatever its
        // Interrupt Others buttons say - they are about the others.
        if (current == animationName && player.IsPlaying(animationName))
        {
            Press(animationName == Travel
                ? "Already running: stops where it is and starts over from the left. On Complete +0."
                : "Already running: stops where it is and starts swinging again from the left.");
        }
        else
        {
            Press(animationName == Travel
                ? "Crosses in 2.5 s, then DONE shows with a jingle. On Complete +1, ends Completed."
                : "Swings back and forth until something ends it.");
        }

        current = animationName;
        ended = "-";

        // Only the latest play's end is shown: restarting reports Interrupted for the play it replaces.
        int play = ++plays;
        player.Play(animationName, reason =>
        {
            if (play == plays) ended = reason.ToString();
        });
    }

    private void EndCurrent(System.Action<string> end, string whenTravel, string whenEndless)
    {
        if (Running(whenTravel, whenEndless)) end(current);
    }

    private void Interrupt(string animationName, string whenTravel, string whenEndless)
    {
        Running(whenTravel, whenEndless);
        player.Play(animationName);
    }

    private bool Running(string whenTravel, string whenEndless)
    {
        bool running = current != null && player.IsPlaying(current);

        if (!running) Press("Nothing is running - press Play Travel or Play Endless first.");
        else Press(current == Travel ? whenTravel : whenEndless);

        return running;
    }

    private void Press(string expected)
    {
        onCompleteAtPress = onCompleteCount;
        expect.text = "Expect: " + expected;
    }

    private void Update()
    {
        string state = current == null ? "nothing played yet"
            : current + (player.IsPlaying(current) ? ": running" : ": ended " + ended);

        status.text = state
            + "     On Complete: " + onCompleteCount + " (+" + (onCompleteCount - onCompleteAtPress) + ")"
            + "     DONE: " + (done.activeSelf ? "shown" : "hidden")
            + "     runner x: " + Mathf.RoundToInt(runner.anchoredPosition.x);
    }
}
