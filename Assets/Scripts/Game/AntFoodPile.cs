using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
    
    [ItemCanBeNull] private Dictionary<Vector3, Ant> places = new();
    private List<MeshFilter> children;

    private int AvailableFood()
    {
        return FoodLeft - places.Count(p => p.Value);
    }
    
    public override void AntBeginInteract(Ant ant)
    {
        antTimers.Add(ant, 0);
    }

    public override bool CanAntInteract(Ant ant)
    {
        return AvailableFood() > 0 && !ant.carriedObject;
    }

    public override Vector3 GetAntInteractPos(Ant ant)
    {
        return AssignToPoint(places, ant);
    }

    public override void CancelAntInteract(Ant ant)
    {
        UnassignFromPoint(places, ant);
    }

    public override void AntEndInteract(Ant ant)
    {
        antTimers.Remove(ant);
        UnassignFromPoint(places, ant);
    }

    private void Start()
    {
        children = new();
        foreach (MeshFilter model in GetComponentsInChildren<MeshFilter>())
        {
            children.Add(model);
        }
        children.RemoveAt(0);
        float rad = GetBestRadius(out Vector3 center);
        places = GetRadialPlaces(FoodLeft, rad, center);
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
            
            if (children.Count > 0)
            {
                MeshFilter deleted = children[0];
                children.RemoveAt(0);
                Destroy(deleted.gameObject);
            }

            if (FoodLeft <= 0)
            {
                Destroy(gameObject);
            }
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
