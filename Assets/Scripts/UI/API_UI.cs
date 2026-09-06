using System;
using UnityEngine;


// written by andy (rmfz/rmfandyplayz)
// an API that lets gameplay control certain UI behaviors
// also lets UI broadcast pause signals
public class API_UI : MonoBehaviour
{
    public event Action PauseRequested;
    public event Action ResumeRequested;
    public event Action StartRequested;

    [SerializeField] private StatCounter statCounter;
    

    /// <summary>
    /// Requests the game to pause
    /// </summary>
    public void RequestPause()
    {
        PauseRequested?.Invoke();
    }

    /// <summary>
    /// Requests the game to resume
    /// </summary>
    public void RequestResume()
    {
        ResumeRequested?.Invoke();
    }

    /// <summary>
    /// Requests the game to start to the gameplay code
    /// </summary>
    public void RequestStart()
    {
        StartRequested?.Invoke();
    }

    /// <summary>
    /// Updates the "ants on screen" counter
    /// </summary>
    /// <param name="newAntCount"></param>
    public void UpdateAntCount(int newScreenAntCount, int newTotalAntCount)
    {
        statCounter.UpdateAntCount(newScreenAntCount, newTotalAntCount);
    }

    /// <summary>
    /// Updates the "## food" counter
    /// </summary>
    /// <param name="newFoodCount"></param>
    public void UpdateFoodCount(int newFoodCount)
    {
        statCounter.UpdateFoodCount(newFoodCount);
    }

}
