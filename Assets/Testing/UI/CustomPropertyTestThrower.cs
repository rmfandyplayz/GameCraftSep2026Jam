using System;
using UnityEngine;

/// <summary>
/// Hand-test fixture for the DOTween UI player's Custom Property step (UITester.unity, station V1).
/// A property whose getter always throws: the step using it should warn once and be skipped, and
/// every other step on the same player should still play.
/// </summary>
public class CustomPropertyTestThrower : MonoBehaviour
{
    public float Explodes
    {
        get { throw new InvalidOperationException("this getter throws on purpose"); }
        set { }
    }
}
