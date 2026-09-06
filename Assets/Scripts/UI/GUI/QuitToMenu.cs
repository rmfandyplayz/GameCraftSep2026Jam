using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


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
            Debug.LogError("not implemented yet. return to main menu somehow");
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
