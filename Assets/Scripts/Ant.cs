using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum EAntState
{
    Idle,
    DirectMove,
    PathMove,
    Acting
}

public class Ant : MonoBehaviour
{
    [FormerlySerializedAs("Hunger")] [SerializeField] private float MaxHunger;
    [SerializeField] private float hunger;
    private Vector3 currentDirectedPos;
    [SerializeField] private float Acceleration;
    [FormerlySerializedAs("moveSpeed")] [SerializeField] private float MoveSpeed;

    private Rigidbody rb;
    public AntNest myNest { get; private set; }

    private NavMeshPath path;
    private int pathNode;
    private float stuckTimer;

    [NonSerialized] public bool returningToNest;

    [NonSerialized] public List<AntInteractable> nearbyInteractables = new();
    
    public EAntState State { get; private set; }

    private AntInteractable currentInteractable;
    public AntCarriableObject carriedObject { get; private set; }

    private static NavMeshQueryFilter antNavMeshQueryFilter;

    public static int GetNavMeshID(string name)
    {
        for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
        {
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(i);
            if (name == NavMesh.GetSettingsNameFromID(settings.agentTypeID))
            {
                return settings.agentTypeID;
            }
        }

        return 0;
    }
    
    public void Direct(Vector3 pos)
    {
        if (returningToNest)
            return;
        
        currentDirectedPos = pos;
        State = EAntState.DirectMove;
        stuckTimer = 0;
    }

    public void GoIdle()
    {
        State = EAntState.Idle;
    }

    public void DirectPathfind(Vector3 pos)
    {
        State = EAntState.PathMove;
        
        NavMesh.SamplePosition(transform.position, out NavMeshHit srcHit, 999, antNavMeshQueryFilter);
        NavMesh.SamplePosition(pos, out NavMeshHit dstHit, 999, antNavMeshQueryFilter);
        path = new NavMeshPath();
        
        if (NavMesh.CalculatePath(srcHit.position, dstHit.position, antNavMeshQueryFilter, path))
        {
            pathNode = 0;
        }
        else
        {
            GoIdle();
        }
    }

    public void ReturnToNest()
    {
        DirectPathfind(myNest.transform.position);
        returningToNest = true;
    }

    private void UnstuckMe()
    {
        if (returningToNest)
        {
            // PANIC
            transform.position = myNest.transform.position;
            ReturnToNest();
        }
        else
        {
            // probably running into a wall or something - just return to idle.
            GoIdle();
        }

        stuckTimer = 0;
    }

    private bool CloseToTarget(Vector3 target)
    {
        Vector3 dist = (transform.position - target);
        dist.y = 0;
        
        return dist.magnitude < 0.2f;
    }

    private void MoveTowards(Vector3 target)
    {
        if (CloseToTarget(target))
        {
            if (rb.linearVelocity.magnitude < 0.01 && State != EAntState.PathMove)
            {
                rb.linearVelocity = Vector3.zero;
                GoIdle();
            }
            return;
        }
        StuckCheck();

        // dont go up or down lol then we get flying ants
        Vector3 accel = (target - transform.position).normalized * (Acceleration * Time.deltaTime);
        accel.y = 0;
        
        rb.AddForce(accel);
        
        // Clamp max speed
        Vector3 horizVel = rb.linearVelocity;
        horizVel.y = 0;
        if (horizVel.magnitude > MoveSpeed)
        {
            horizVel = horizVel.normalized * MoveSpeed;
        }
        rb.linearVelocity = new Vector3(horizVel.x, rb.linearVelocity.y, horizVel.z);

        // Hunger loss
        hunger -= horizVel.magnitude * Time.deltaTime;
        HungerCheck();
    }

    private void HungerCheck()
    {
        if (hunger <= 0 && !returningToNest)
        {
            ReturnToNest();
        }
    }

    private void StuckCheck()
    {
        if (rb.linearVelocity.magnitude < 0.01)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 1)
                UnstuckMe();
        }
        else
            stuckTimer = 0;
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
                returningToNest = false;
                GoIdle();
                return;
            }
            curGoal = path.corners[pathNode];
        }

        MoveTowards(curGoal);
    }

    private void Start()
    {
        currentDirectedPos = transform.position;
        rb = GetComponent<Rigidbody>();
        hunger = MaxHunger;
        
        antNavMeshQueryFilter = new NavMeshQueryFilter()
        {
            areaMask = NavMesh.AllAreas,
            agentTypeID = GetNavMeshID("Ant")
        };
    }

    public void InteractWith(AntInteractable interactable)
    {
        State = EAntState.Acting;
        interactable.AntBeginInteract(this);
        currentInteractable = interactable;
    }

    public void ReleaseInteract(AntInteractable interactable)
    {
        currentInteractable = null;
        interactable.AntEndInteract(this);
        GoIdle();
    }

    public void GrabObject(AntCarriableObject toCarry)
    {
        carriedObject = toCarry;
        carriedObject.OnPickup();
        ReturnToNest();
    }

    private void DepositObject(AntNest depositNest)
    {
        carriedObject.OnDeposit(depositNest);
        
        carriedObject = null;
    }

    private void IdleTick()
    {
        HungerCheck();
        
        AntInteractable first = nearbyInteractables.FirstOrDefault(interactable => interactable.CanAntInteract(this));
        if (first)
            InteractWith(first);
    }

    private void Update()
    {
        switch (State)
        {
            case EAntState.Idle:
                IdleTick();
                break;
            case EAntState.DirectMove:
                MoveTowards(currentDirectedPos);
                break;
            case EAntState.PathMove:
                PathFind();
                break;
            case EAntState.Acting:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (Vector3.Distance(transform.position, myNest.transform.position) < 0.5)
        {
            NearNest();
        }

        if (carriedObject)
        {
            carriedObject.transform.position = transform.position + Vector3.up;
        }
    }

    private void NearNest()
    {
        hunger = MaxHunger;
        if (carriedObject)
        {
            DepositObject(myNest);
        }

        if (returningToNest)
        {
            returningToNest = false;
            GoIdle();
        }
    }

    private void OnEnable()
    {
        myNest = FindAnyObjectByType<AntNest>();
        myNest.AddAnt(this);
    }

    private void OnDisable()
    {
        myNest.RemoveAnt(this);
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = Color.white;
        Handles.Label(transform.position + Vector3.up * 2, hunger.ToString(CultureInfo.InvariantCulture));
        
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
