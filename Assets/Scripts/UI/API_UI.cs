using System;
using UnityEngine;


// written by andy (rmfz/rmfandyplayz)
// an API that lets gameplay control certain UI behaviors
// also lets UI broadcast pause signals
public class API_UI : MonoBehaviour
{
    public event Action PauseRequested;
    public event Action ResumeRequested;

    [SerializeField] private AntCounter antCounter;

    public void RequestPause()
    {
        PauseRequested?.Invoke();
    }

    public void RequestResume()
    {
        ResumeRequested?.Invoke();
    }

    public void UpdateAntCount(int newAntCount)
    {
        antCounter.UpdateAntCount(newAntCount);
    }

}
