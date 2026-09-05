using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Ant : MonoBehaviour
{
    [FormerlySerializedAs("hunger")] [SerializeField] private int Hunger;
    private Vector3 currentDirectedPos;
    [SerializeField] private float Acceleration;
    [FormerlySerializedAs("moveSpeed")] [SerializeField] private float MoveSpeed;

    private Rigidbody rb;
    private AntNest nest;

    private NavMeshPath path;
    private int pathNode;
    
    public void Direct(Vector3 pos)
    {
        currentDirectedPos = pos;
        path = null;
    }

    public void DirectPathfind(Vector3 pos)
    {
        NavMesh.SamplePosition(transform.position, out NavMeshHit srcHit, 999, NavMesh.AllAreas);
        NavMesh.SamplePosition(pos, out NavMeshHit dstHit, 999, NavMesh.AllAreas);
        path = new NavMeshPath();
        
        if (NavMesh.CalculatePath(srcHit.position, dstHit.position, NavMesh.AllAreas, path))
        {
            pathNode = 0;
        }
        else
        {
            path = null;
        }
    }

    public void ReturnToNest()
    {
        DirectPathfind(nest.transform.position);
    }

    private bool CloseToTarget(Vector3 target)
    {
        Vector3 dist = (transform.position - target);
        dist.y = 0;
        
        return dist.magnitude < (MoveSpeed * Time.deltaTime * 4);
    }

    private void MoveTowards(Vector3 target)
    {
        if (CloseToTarget(target))
            return;

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
                path = null;
                currentDirectedPos = transform.position;
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
    }

    private void Update()
    {
        if(path == null)
            MoveTowards(currentDirectedPos);
        else
            PathFind();
    }

    private void OnEnable()
    {
        nest = FindAnyObjectByType<AntNest>();
        nest.AddAnt(this);
        ReturnToNest();
    }

    private void OnDisable()
    {
        nest.RemoveAnt(this);
    }

    private void OnDrawGizmos()
    {
        if (path != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLineStrip(path.corners, false);
            Gizmos.color = Color.purple;
            Gizmos.DrawLine(transform.position, path.corners[pathNode]);
        }
    }
}
