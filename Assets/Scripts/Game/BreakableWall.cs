using System.Collections.Generic;
using UnityEngine;

public class BreakableWall : AntInteractable
{
    private List<Ant> antsInteracting;
    [SerializeField] private int antsNeeded;
    private bool destroying = false;

    void Start()
    {
        antsInteracting = new();
    }
    public override void AntBeginInteract(Ant ant)
    { 
        if (!antsInteracting.Contains(ant))
        {
            antsInteracting.Add(ant);
                    if (antsInteracting.Count >= antsNeeded)
                    {
                        destroying = true;
                        for (int i = antsInteracting.Count - 1; i >= 0; i--)
                        {
                            AntEndInteract(antsInteracting[i]);
                        }
                        Destroy(this.gameObject);
                    }
        }
    }

    public override bool CanAntInteract(Ant ant)
    {
        return !destroying;
    }

    public override Vector3 GetAntInteractPos(Ant ant)
    {
        return transform.position;
    }

    public override void AntEndInteract(Ant ant)
    {
        antsInteracting.Remove(ant);
    }
}
