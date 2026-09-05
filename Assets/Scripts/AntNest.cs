using System;
using System.Collections.Generic;
using UnityEngine;

public class AntNest : MonoBehaviour
{
    [NonSerialized] public int foodCount;

    private List<Ant> ants = new();
    
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
    
}
