using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hand-test fixture for the DOTween UI player's Custom Property step (UITester.unity, station S1).
/// Health and Coins are C# properties whose setters redraw the gauge, so tweening them shows
/// straight away, in the edit-mode preview too.
///
/// The last three members are here to NOT show up in the Property dropdown: a read-only property,
/// an [Obsolete] one and a type the step cannot tween.
/// </summary>
public class CustomPropertyTestGauge : MonoBehaviour
{
    [SerializeField] private Image bar;
    [SerializeField] private TMP_Text label;
    [SerializeField] private float health = 75f;
    [SerializeField] private int coins = 12;

    public float Health
    {
        get { return health; }
        set { health = Mathf.Clamp(value, 0f, 100f); Redraw(); }
    }

    public int Coins
    {
        get { return coins; }
        set { coins = Mathf.Max(0, value); Redraw(); }
    }

    public float HealthFraction
    {
        get { return health / 100f; }
    }

    [Obsolete("Only here to check the Property dropdown leaves [Obsolete] members out.")]
    public float OldHealth
    {
        get { return health; }
        set { health = value; }
    }

    public bool IsAlive
    {
        get { return health > 0f; }
        set { if (!value) Health = 0f; }
    }

    public void Redraw()
    {
        if (bar != null) bar.fillAmount = health / 100f;
        if (label != null) label.text = "HP " + Mathf.RoundToInt(health) + "     " + coins + " coins";
    }
}
