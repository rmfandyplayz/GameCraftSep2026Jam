using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

public enum EAntPathState
{
    ReturnToHome,
    ActPath
}

public class Ant : MonoBehaviour
{
    [SerializeField] private float hunger;
    private Vector3 currentDirectedPos;
    [SerializeField] private float Acceleration;
    [FormerlySerializedAs("moveSpeed")] [SerializeField] private float MoveSpeed;

    [SerializeField] private float AntAvoidRange;
    [SerializeField] private float AntAvoidForce;

    private Rigidbody rb;
    public AntNest myNest { get; private set; }

    private NavMeshPath path;
    private int pathNode;
    private float stuckTimer;
    private int stuckCount;

    [NonSerialized] public bool returningToNest;

    [NonSerialized] public List<AntInteractable> nearbyInteractables = new();
    
    public EAntState State { get; private set; }
    public EAntPathState PathState { get; private set; }

    private AntInteractable currentInteractable;
    public AntCarriableObject carriedObject { get; private set; }

    private static NavMeshQueryFilter antNavMeshQueryFilter;
    private static readonly int BaseColorPropID = Shader.PropertyToID("_BaseColor");


    private Renderer _renderer;
    protected MaterialPropertyBlock matPropBlock;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color hungryColor;

    private float lastDirectTime = 0;

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

    private IEnumerable<Ant> GetAntsInRange(float distance)
    {
        distance *= distance;
        return myNest.GetAnts().Where(a => a != this && (a.transform.position - transform.position).sqrMagnitude <= distance);
    }
    
    public void Direct(Vector3 pos)
    {
        if (State == EAntState.PathMove)
            return;

        lastDirectTime = Time.time;
        
        if (State == EAntState.Acting)
        {
            ReleaseInteract(currentInteractable);
        }
        
        currentDirectedPos = pos;
        State = EAntState.DirectMove;
        stuckTimer = 0;
        stuckCount = 0;
    }

    public void GoIdle()
    {
        State = EAntState.Idle;
        stuckCount = 0;
        stuckTimer = 0;
    }

    public void DirectPathfind(Vector3 pos, EAntPathState pathingType)
    {
        if (State == EAntState.Acting)
        {
            ReleaseInteract(currentInteractable);
        }
        
        State = EAntState.PathMove;
        PathState = pathingType;
        
        NavMesh.SamplePosition(transform.position, out NavMeshHit srcHit, 999, antNavMeshQueryFilter);
        NavMesh.SamplePosition(pos, out NavMeshHit dstHit, 999, antNavMeshQueryFilter);
        path = new NavMeshPath();
        
        if (NavMesh.CalculatePath(srcHit.position, dstHit.position, antNavMeshQueryFilter, path))
        {
            pathNode = 0;
        }
        else if(State != EAntState.Acting)
        {
            GoIdle();
        }
    }

    public void InteractWith(AntInteractable interactable)
    {
        DirectPathfind(interactable.GetAntInteractPos(this), EAntPathState.ActPath);
        currentInteractable = interactable;
    }

    public void ArriveInteract()
    {
        State = EAntState.Acting;
        currentInteractable.AntBeginInteract(this);
    }
    
    public void ReturnToNest()
    {
        DirectPathfind(myNest.transform.position, EAntPathState.ReturnToHome);
        returningToNest = true;
    }

    private void UnstuckMe()
    {
        if (State == EAntState.PathMove)
        {
            if (stuckCount > 5)
            {
                transform.position = path.corners.Last();
            }
            DirectPathfind(path.corners.Last(), PathState);
            stuckCount++;
        }
        else if(State != EAntState.Acting)
        {
            // probably running into a wall or something - just return to idle.
            GoIdle();
            stuckCount = 0;
        }

        stuckTimer = 0;
    }

    private bool CloseToTarget(Vector3 target, float distance = 0.2f)
    {
        Vector3 dist = (transform.position - target);
        dist.y = 0;
        
        return dist.sqrMagnitude < distance * distance;
    }

    public void MoveTowards(Vector3 target)
    {
        if (CloseToTarget(target, AntAvoidRange * 2))
        {
            if (State == EAntState.DirectMove)
            {
                GoIdle();
                return;
            }
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
        if(State != EAntState.Acting)
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
                // End of path
                switch (PathState)
                {
                    case EAntPathState.ReturnToHome:
                        returningToNest = false;
                        GoIdle();
                        return;
                    case EAntPathState.ActPath:
                        ArriveInteract();
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            curGoal = path.corners[pathNode];
        }

        MoveTowards(curGoal);
    }

    private void Start()
    {
        currentDirectedPos = transform.position;
        rb = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();
        hunger = myNest.GetMaxHunger();
        
        antNavMeshQueryFilter = new NavMeshQueryFilter()
        {
            areaMask = NavMesh.AllAreas,
            agentTypeID = GetNavMeshID("Ant")
        };

        matPropBlock = new();
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

        Vector3 decel = -rb.linearVelocity;
        decel.y = 0;
        decel.Normalize();
        decel *= Time.deltaTime * Acceleration;

        if (decel.sqrMagnitude <= .01)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
        else
        {
            rb.AddForce(decel);
        }
        
        
        
        CheckForInteractables();
    }

    private void DirectTick()
    {
        MoveTowards(currentDirectedPos);
        if (Time.time > lastDirectTime + 0.5f)
        {
            CheckForInteractables();
        }
    }

    private void CheckForInteractables()
    {
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
                DirectTick();
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

        float avoidRangeSqrd = AntAvoidRange * AntAvoidRange * 4;

        foreach (Ant ant in GetAntsInRange(AntAvoidRange * 2))
        {
            Vector3 offset = transform.position - ant.transform.position;
            offset.y = 0;
            float force = offset.sqrMagnitude;
            force /= avoidRangeSqrd;
            force = 1 - force;
            force *= AntAvoidForce;
            
            rb.AddForce(offset.normalized * (force * Time.deltaTime));
        }

        SetAntAngle();
        
        _renderer.GetPropertyBlock(matPropBlock);
        matPropBlock.SetColor(BaseColorPropID, Color.Lerp(hungryColor, defaultColor, hunger / myNest.GetMaxHunger()));
        _renderer.SetPropertyBlock(matPropBlock);
    }

    private void SetAntAngle()
    {
        var vel = rb.linearVelocity;
        vel.y = 0;
        vel.Normalize();
        float angle = Mathf.Atan2(vel.z, vel.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, -angle, 0);
    }

    private void NearNest()
    {
        hunger = myNest.GetMaxHunger();
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

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AntAvoidRange);
        
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
