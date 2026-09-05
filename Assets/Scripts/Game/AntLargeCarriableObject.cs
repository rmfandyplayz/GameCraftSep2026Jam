using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class AntLargeCarriableObject : AntInteractable
{

    [SerializeField] private int MinAntsForCarry;
    [SerializeField] private int MaxAntsForCarry;

    [SerializeField] private float BaseCarrySpeed;
    [SerializeField] private float MaxCarrySpeed;
    
    [SerializeField] private float Acceleration;


    private HashSet<Ant> antsCarrying = new();

    private NavMeshPath path;
    private int pathNode;
    private bool carrying;

    private Rigidbody rb;

    protected abstract void DepositToNest(AntNest nest);

    private static NavMeshQueryFilter navMeshQueryFilter;
    


    private int GetAntCarryCount()
    {
        return antsCarrying.Count;
    }

    public override void AntBeginInteract(Ant ant)
    {
        antsCarrying.Add(ant);
        DirectAntToMe(ant);
    }
    public override bool CanAntInteract(Ant ant){
        return GetAntCarryCount() < MaxAntsForCarry;
    }
    public override void AntEndInteract(Ant ant)
    {
        antsCarrying.Remove(ant);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void StartCarrying()
    {
        carrying = true;
        path = new();
        
        NavMesh.SamplePosition(transform.position, out NavMeshHit srcHit, 999, navMeshQueryFilter);
        NavMesh.SamplePosition(antsCarrying.First().myNest.transform.position, out NavMeshHit dstHit, 999, navMeshQueryFilter);
        path = new NavMeshPath();
        
        if (NavMesh.CalculatePath(srcHit.position, dstHit.position, navMeshQueryFilter, path))
        {
            pathNode = 0;
        }
        else
        {
            // fuck
            Debug.LogWarning("CANT RETURN ITEM TO BASE");
        }
    }

    private void StopCarrying()
    {
        carrying = false;
    }

    private void DirectAntToMe(Ant ant)
    {
        ant.Direct(transform.position);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        navMeshQueryFilter = new NavMeshQueryFilter()
        {
            areaMask = NavMesh.AllAreas,
            agentTypeID = Ant.GetNavMeshID("LargeObject")
        };
    }

    private void Update()
    {
        if (!carrying)
        {
            if (GetAntCarryCount() >= MinAntsForCarry)
            {
                StartCarrying();
            }
        }
        else
        {
            if (GetAntCarryCount() < MinAntsForCarry)
            {
                StopCarrying();
            }
        }

        if (carrying)
        {
            PathFind();
        }
        
        foreach (Ant ant in antsCarrying)
        {
            DirectAntToMe(ant);
        }
    }
    
    private bool CloseToTarget(Vector3 target)
    {
        Vector3 dist = (transform.position - target);
        dist.y = 0;
        
        return dist.magnitude < 1f;
    }

    private void MoveTowards(Vector3 target)
    {
        float speed = GetCarrySpeed();


        // dont go up or down lol then we get flying ants
        Vector3 accel = (target - transform.position).normalized * (Acceleration * Time.deltaTime);
        accel.y = 0;
        
        rb.AddForce(accel);
        
        // Clamp max speed
        Vector3 horizVel = rb.linearVelocity;
        horizVel.y = 0;
        if (horizVel.magnitude > speed)
        {
            horizVel = horizVel.normalized * speed;
        }
        rb.linearVelocity = new Vector3(horizVel.x, rb.linearVelocity.y, horizVel.z);
    }

    protected float GetCarrySpeed()
    {
        float a = Mathf.InverseLerp(MinAntsForCarry, MaxAntsForCarry, GetAntCarryCount());
        float b = Mathf.Lerp(BaseCarrySpeed, MaxCarrySpeed, a);
        return b;
    }

    private void PathFind()
    {
        Vector3 curGoal = path.corners[pathNode];
        if (CloseToTarget(curGoal))
        {
            pathNode++;
            if (pathNode == path.corners.Length)
            {
                // end of path
                DepositToNest(antsCarrying.First().myNest);
                foreach (Ant ant in antsCarrying.ToList())
                {
                    ant.ReleaseInteract(this);
                }
                Destroy(gameObject);
                return;
            }
            curGoal = path.corners[pathNode];
        }

        MoveTowards(curGoal);
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = Color.white;
        Handles.Label(transform.position + Vector3.up * 2, GetAntCarryCount().ToString());
        
        if (path != null && pathNode < path.corners.Length)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLineStrip(path.corners, false);
            Gizmos.color = Color.purple;
            Gizmos.DrawLine(transform.position, path.corners[pathNode]);
        }
    }
#endif
}
