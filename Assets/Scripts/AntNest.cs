using System.Collections.Generic;
using UnityEngine;

public class AntNest : MonoBehaviour
{
    [HideInInspector] public int foodCount
    {
        get { return foodCount; }
        set { foodCount = value; }
    }

    private List<Ant> ants;
    
    void Start()
    {
        ants = new();
    }

    public void AddAnt(Ant ant)
    {
        ants.Add(ant);
    }
    
}
