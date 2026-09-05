using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Ant : MonoBehaviour
{
    [FormerlySerializedAs("hunger")] [SerializeField] private int Hunger;
    private Vector3 currentDirectedPos;
    [SerializeField] private float Acceleration;
    [FormerlySerializedAs("moveSpeed")] [SerializeField] private float MoveSpeed;

    private Rigidbody rb;
    private AntNest nest;
    
    public void Direct(Vector3 pos)
    {
        currentDirectedPos = pos;
    }

    private bool CloseToTarget(Vector3 target)
    {
        return (transform.position - target).magnitude < (MoveSpeed * Time.deltaTime * 2);
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

    private void Start()
    {
        currentDirectedPos = transform.position;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        MoveTowards(currentDirectedPos);
    }

    private void OnEnable()
    {
        nest = FindAnyObjectByType<AntNest>();
        nest.AddAnt(this);
    }

    private void OnDisable()
    {
        nest.RemoveAnt(this);
    }
}
