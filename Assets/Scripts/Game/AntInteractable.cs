
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class AntInteractable : MonoBehaviour
{
    public abstract void AntBeginInteract(Ant ant);
    public abstract bool CanAntInteract(Ant ant);
    public abstract Vector3 GetAntInteractPos(Ant ant);
    public abstract void AntEndInteract(Ant ant);

    private void OnTriggerEnter(Collider other)
    {
        var ant = other.GetComponent<Ant>();
        if (!ant)
            return;

        ant.nearbyInteractables.Add(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var ant = other.GetComponent<Ant>();
        if (!ant)
            return;

        ant.nearbyInteractables.Remove(this);
    }

    protected Dictionary<Vector3, Ant> GetRadialPlaces(float count, float radius)
    {
        Dictionary<Vector3, Ant> places = new();
        for (int i = 0; i < count; i++)
        {
            float angle = i / count;
            angle *= 360;

            Vector3 pos = Vector3.forward;
            pos = Quaternion.AngleAxis(angle, Vector3.up) * pos;
            pos *= radius;

            pos += transform.position;
            places.Add(pos, null);
        }

        return places;
    }

    protected Vector3 AssignToPoint(Dictionary<Vector3, Ant> places, Ant ant)
    {
        var possiblePlaces = places.Where(p => !p.Value);
        Vector3 closest = Vector3.zero;
        float bestDist = float.MaxValue;
        foreach (var possiblePlace in possiblePlaces)
        {
            float dist = (transform.position - possiblePlace.Key).sqrMagnitude;
            if (dist < bestDist)
            {
                closest = possiblePlace.Key;
                bestDist = dist;
            }
        }

        if (closest == Vector3.zero)
        {
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Debug.LogError("FUCK");
            return closest;
        }

        places[closest] = ant;

        return closest;
    }
    
    readonly private float radBuffer = 0.4f;
    protected float GetBestRadius()
    {
        float scaleFactor = Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
        
        var sphere = GetComponents<SphereCollider>().FirstOrDefault(s => !s.isTrigger);
        if (sphere)
        {
            return sphere.radius * scaleFactor + radBuffer;
        }

        var capsule = GetComponents<CapsuleCollider>().FirstOrDefault(s => !s.isTrigger);
        if (capsule)
        {
            return (capsule.radius + capsule.height) * scaleFactor + radBuffer;
        }

        var box = GetComponents<BoxCollider>().FirstOrDefault(s => !s.isTrigger);
        if (box)
        {
            float w = box.size.x;
            float h = box.size.z;
            return Mathf.Sqrt(w * w + h * h) * 0.5f * scaleFactor + radBuffer;
        }

        Debug.LogWarning("GetBestRadiusFailed!");
        return 0;
    }
}