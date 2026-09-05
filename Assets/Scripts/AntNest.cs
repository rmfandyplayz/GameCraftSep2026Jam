using System.Collections.Generic;
using UnityEngine;

public class AntNest : MonoBehaviour
{
    [HideInInspector] public int foodCount
    {
        get { return foodCount; }
        set { foodCount = value; }
    }

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
