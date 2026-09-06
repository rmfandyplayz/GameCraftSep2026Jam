using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BreakableWall : AntInteractable
{
    private List<Ant> antsInteracting = new();
    [SerializeField] private int antsNeeded;
    private bool destroying = false;

    private Dictionary<Vector3, Ant> frontPlaces = new(); 
    private Dictionary<Vector3, Ant> backPlaces = new();

    [SerializeField] private TextMeshPro wallText;
    
    void Start()
    {
        GeneratePlaces();
        wallText.text = antsNeeded.ToString();
    }

    private void Update()
    {
        foreach (Ant ant in antsInteracting)
        {
            if (IsPointInFront(ant.transform.position))
            {
                ant.SetAntAngle(-transform.forward);
            }
            else
            {
                ant.SetAntAngle(transform.forward);
            }
        }
    }

    private void GeneratePlaces()
    {
        frontPlaces.Clear();
        backPlaces.Clear();
        
        BoxCollider wallCollider = GetComponents<BoxCollider>().First(p => !p.isTrigger);
        float wallWidth = wallCollider.size.x * transform.lossyScale.x;
        float wallDepth = wallCollider.size.z * transform.lossyScale.z;

        float minPos = -wallWidth * 0.5f + wallWidth / antsNeeded;
        float maxPos = wallWidth * 0.5f - wallWidth / antsNeeded;

        float wallOffset = wallDepth/2 + Ant.RadBuffer;
        float yOffset = wallCollider.size.y * -0.5f * transform.lossyScale.y;
        for (int i = 0; i < antsNeeded; i++)
        {
            float fac = i / (float)(antsNeeded-1);

            var frontPos = new Vector3(Mathf.Lerp(minPos, maxPos, fac), yOffset, wallOffset);
            frontPos = transform.rotation * frontPos;
            frontPos += transform.position;
            
            var backPos = new Vector3(Mathf.Lerp(minPos, maxPos, fac), yOffset, -wallOffset);
            backPos = transform.rotation * backPos;
            backPos += transform.position;
            
            frontPlaces.Add(frontPos, null);
            backPlaces.Add(backPos, null);
        }
    }

    private bool IsPointInFront(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        float dotProduct = Vector3.Dot(transform.forward, offset.normalized);
        return dotProduct > 0;
    }
    
    public override void AntBeginInteract(Ant ant)
    { 
        if (!antsInteracting.Contains(ant))
        {
            antsInteracting.Add(ant);
            if (antsInteracting.Count >= antsNeeded)
            {
                destroying = true;
                foreach (Ant inAnt in antsInteracting.ToList())
                {
                    inAnt.ReleaseInteract(this);
                }
                Destroy(this.gameObject);
            }
        }
    }

    public override bool CanAntInteract(Ant ant)
    {
        if (destroying)
            return false;

        return IsPointInFront(ant.transform.position) ? frontPlaces.ContainsValue(null) : backPlaces.ContainsValue(null);
    }

    public override Vector3 GetAntInteractPos(Ant ant)
    {
        return AssignToPoint(IsPointInFront(ant.transform.position) ? frontPlaces : backPlaces, ant);
    }

    public override void AntEndInteract(Ant ant)
    {
        antsInteracting.Remove(ant);
        UnassignFromPoint(IsPointInFront(ant.transform.position) ? frontPlaces : backPlaces, ant);
    }
    
    public override void CancelAntInteract(Ant ant)
    {
        UnassignFromPoint(IsPointInFront(ant.transform.position) ? frontPlaces : backPlaces, ant);
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = Color.white;
        Handles.Label(transform.position + Vector3.up * 2, antsInteracting.Count.ToString());
    }

    private void OnDrawGizmosSelected()
    {
        if (frontPlaces.Count != antsNeeded)
        {
            GeneratePlaces();
        }
        
        Gizmos.color = Color.red;
        foreach (Vector3 place in frontPlaces.Keys)
        {
            Gizmos.DrawSphere(place, 0.1f);
        }
        
        Gizmos.color = Color.blue;
        foreach (Vector3 place in backPlaces.Keys)
        {
            Gizmos.DrawSphere(place, 0.1f);
        }

        Gizmos.color = Color.purple;
        foreach (Ant ant in antsInteracting)
        {
            Gizmos.DrawLine(transform.position, ant.transform.position);
        }
    }
    #endif
}
