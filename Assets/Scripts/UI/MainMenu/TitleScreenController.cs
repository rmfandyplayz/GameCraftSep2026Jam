using System;
using UnityEngine;


// written by andy (rmfandyplayz/rmfz)
// simple title screen script to facilitate certain events
public class TitleScreenController : MonoBehaviour
{
    [SerializeField] UIAnimationPlayer player;

    private void Start()
    {
        player.PlayAnimation("SceneTransIn");
    }



    public void ExitGame()
    {
        Application.Quit();
    }
}
