using TMPro;
using UnityEngine;


// written by andy (rmfz/rmfandyplayz)
// simple script to change the ant & food counter text
public class StatCounter : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI antCountText;
    [SerializeField] TextMeshProUGUI foodCountText;
    [SerializeField, Tooltip("update this when u add the actual ant icon")]
    int spriteIndex;

    private int screenAnts;
    private int totalAnts;


    public void UpdateFoodCount(int newFoodCount)
    {
        foodCountText.text = $"{newFoodCount} food";
    }
    
    public void UpdateAntCount(int newScreenAntCount, int newTotalAntCount)
    {
        screenAnts = newScreenAntCount;
        totalAnts = newTotalAntCount;
        UpdateText();
    }


    private void UpdateText()
    {
        if (screenAnts == 1 && totalAnts == 1)
        {
            antCountText.text = $"<sprite index={spriteIndex}> 1 ant on screen  •  1 ant total";
        }
        else if(screenAnts == 1 && totalAnts != 1)
        {
            antCountText.text = $"<sprite index={spriteIndex}> 1 ant on screen  •  {totalAnts} ants total";
        }
        else if(screenAnts != 1 && totalAnts == 1)
        {
            antCountText.text = $"<sprite index={spriteIndex}> {screenAnts} ants on screen  •  1 ant total";
        }
        else if(screenAnts != 1 && totalAnts != 1)
        {
            antCountText.text = $"<sprite index={spriteIndex}> {screenAnts} ants on screen  •  {totalAnts} ants total";
        }
    }
}
