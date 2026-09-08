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
    
    private AudioSource jingle;

    [SerializeField] private TextMeshPro wallText;

    private float wallDepth;
    
    void Start()
    {
        jingle = GetComponent<AudioSource>();
        wallText.text = antsNeeded.ToString();
        
        BoxCollider wallCollider = GetComponents<BoxCollider>().First(p => !p.isTrigger);
        wallDepth = wallCollider.size.z * transform.lossyScale.z;
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

    private Vector3 FindPointAlongWallLine(Vector3 srcPoint, Vector3 offset)
    {
        Vector3 dirVec = transform.forward;
        Vector3 planePos = transform.position;
        planePos += transform.rotation * offset;
        
        Vector3 pOffset = srcPoint - planePos;

        float dist = Vector3.Dot(pOffset, dirVec);
        Vector3 point = srcPoint - (dist * dirVec);
        return point;
    }

    private bool IsPointInFront(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        float dotProduct = Vector3.Dot(transform.forward, offset.normalized);
        return dotProduct > 0;
    }

    private float FrontFactor(Vector3 point)
    {
        return IsPointInFront(point) ? 1 : -1;
    }
    
    public override void AntBeginInteract(Ant ant)
    { 
        if (!antsInteracting.Contains(ant))
        {
            antsInteracting.Add(ant);
            ant.SetLockAnt(true);
            if (antsInteracting.Count >= antsNeeded)
            {
                destroying = true;
                foreach (Ant inAnt in antsInteracting.ToList())
                {
                    inAnt.ReleaseInteract(this);
                }

                jingle.Play();
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
        Vector3 antPos = ant.transform.position;
        return FindPointAlongWallLine(antPos, Vector3.forward * (FrontFactor(antPos) * (Ant.RadBuffer + wallDepth)));
    }

    public override void AntEndInteract(Ant ant)
    {
        antsInteracting.Remove(ant);
        ant.SetLockAnt(false);
    }
    
    public override void CancelAntInteract(Ant ant)
    {
        ant.SetLockAnt(false);
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = Color.white;
        Handles.Label(transform.position + Vector3.up * 2, antsInteracting.Count.ToString());
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;
        foreach (Ant ant in antsInteracting)
        {
            Gizmos.DrawLine(transform.position, ant.transform.position);
        }
    }
    #endif
}
