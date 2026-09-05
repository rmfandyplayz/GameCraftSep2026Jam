using System;
using System.Collections.Generic;
using UnityEngine;

public class AntNest : MonoBehaviour
{
    [NonSerialized] public int foodCount;

    private List<Ant> ants = new();
    private List<StoredObject> inventory = new();
    
    void Start()
    {
    }

    public void AddAnt(Ant ant)
    {
        ants.Add(ant);
    }

    public void RemoveAnt(Ant ant)
    {
        ants.Remove(ant);
    }

    public IEnumerable<Ant> GetAnts()
    {
        return ants;
    }

    public void AddToInventory(StoredObject obj)
    {
        inventory.Add(obj);
    }

    public void RemoveFromInventory(StoredObject obj)
    {
        inventory.Remove(obj);
    }
    
}
