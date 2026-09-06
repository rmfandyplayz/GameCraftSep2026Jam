using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


// written by andy (rmfz/rmfandyplayz)
// simple behavior handling for the quit to menu button
public class QuitToMenu : MonoBehaviour
{
    private bool isQuitting = false;
    private string originalText;
    private TextMeshProUGUI quitText;
    [SerializeField] CanvasGroup pauseButtonGroup;

    private void Awake()
    {
        quitText = GetComponent<TextMeshProUGUI>();
        originalText = quitText.text;
    }

    public void QuitGame()
    {
        if (isQuitting)
        {
            // LoadScene does not reset the clock and the pause menu left it at 0, which used
            // to leak a frozen game into the reloaded scene. AntUIHandler.Awake pauses again
            // for the title screen, so the menu still locks input.
            Time.timeScale = 1;
            SceneManager.LoadScene("Level");
            pauseButtonGroup.blocksRaycasts = false;
        }
        else
        {
            isQuitting = true;
            quitText.text = "Confirm?";
        }
    }

    public void ResetState()
    {
        isQuitting = false;
        quitText.text = originalText;
    }
}
