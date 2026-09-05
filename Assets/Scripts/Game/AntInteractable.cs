
using System;
using UnityEngine;

public abstract class AntInteractable : MonoBehaviour
{
    public abstract void AntBeginInteract(Ant ant);
    public abstract bool CanAntInteract(Ant ant);
    public abstract void AntEndInteract(Ant ant);

    private void OnTriggerEnter(Collider other)
    {
        var ant = other.GetComponent<Ant>();
        if (!ant)
            return;

        ant.nearbyInteractables.Add(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var ant = other.GetComponent<Ant>();
        if (!ant)
            return;

        ant.nearbyInteractables.Remove(this);
    }
}