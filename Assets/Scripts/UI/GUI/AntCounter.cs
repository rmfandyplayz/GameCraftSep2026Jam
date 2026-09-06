using TMPro;
using UnityEngine;


// written by andy (rmfz/rmfandyplayz)
// simple script to change the ant counter text
public class AntCounter : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;
    [SerializeField, Tooltip("update this when u add the actual ant icon")]
    int spriteIndex;

    private void Awake()
    {
        if(text == null)
            text = GetComponent<TextMeshProUGUI>();
    }

    
    public void UpdateAntCount(int newAntCount)
    {
        if(newAntCount == 1)
        {
            text.text = $"<sprite index={spriteIndex}> 1 ant on screen";
        }
        else
        {
            text.text = $"<sprite index={spriteIndex}> {newAntCount} ants on screen";
        }
    }

}
