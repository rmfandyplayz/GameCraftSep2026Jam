using System.Collections;
using UnityEngine;

public class Ant : MonoBehaviour
{
    [SerializeField] private int hunger;
    [SerializeField] private Vector3 currentDirectedPos;
    [SerializeField] private float moveSpeed;
    private AntNest nest;
    void Direct(Vector3 pos)
    {
        currentDirectedPos = pos;
        StartCoroutine(MoveToDirectedPos(currentDirectedPos));
    }

    private IEnumerator MoveToDirectedPos(Vector3 goal)
    {
        while (transform.position != goal)
        {
            transform.position = Vector3.MoveTowards(transform.position, goal, moveSpeed);
        }
        yield return null;
    }

    void Start()
    {
        nest = FindAnyObjectByType<AntNest>();
        nest.AddAnt(this);
    }

}
