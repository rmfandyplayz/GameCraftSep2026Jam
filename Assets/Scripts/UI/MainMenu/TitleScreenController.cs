using System;
using UnityEngine;
using rmf_claude.DOTweenUI;


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
