using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AntFoodPile : AntInteractable
{
    private Dictionary<Ant, float> antTimers = new();
    public int FoodLeft;
    [SerializeField] private float GrabTime;

    [SerializeField] private GameObject CrumbPrefab;

    private int AvailableFood()
    {
        return FoodLeft - antTimers.Count;
    }
    
    public override void AntBeginInteract(Ant ant)
    {
        antTimers.Add(ant, 0);
    }

    public override bool CanAntInteract(Ant ant)
    {
        return AvailableFood() > 0 && !ant.carriedObject;
    }

    public override void AntEndInteract(Ant ant)
    {
        antTimers.Remove(ant);
    }

    private void Update()
    {
        foreach (Ant ant in antTimers.Keys.ToList())
        {
            if (ant.State != EAntState.Acting)
            {
                return;
            }
            
            float newTime = antTimers[ant] + Time.deltaTime;
            if (newTime < GrabTime)
            {
                antTimers[ant] = newTime;
                continue;
            }
            FoodLeft--;
            
            ant.ReleaseInteract(this);
            
            GameObject crumbObj = Instantiate(CrumbPrefab);
            var crumbComp = crumbObj.GetComponent<AntFoodCrumb>();
            ant.GrabObject(crumbComp);
            break;
        }
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = Color.white;
        Handles.Label(transform.position + Vector3.up * 2, FoodLeft.ToString());
    }
    #endif
}
