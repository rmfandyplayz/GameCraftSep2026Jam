
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class AntInteractable : MonoBehaviour
{
    public abstract void AntBeginInteract(Ant ant);
    public abstract bool CanAntInteract(Ant ant);
    public abstract Vector3 GetAntInteractPos(Ant ant);
    public abstract void CancelAntInteract(Ant ant);
    public abstract void AntEndInteract(Ant ant);

    private List<Ant> nearbyAnts = new();

    private void OnTriggerEnter(Collider other)
    {
        var ant = other.GetComponent<Ant>();
        if (!ant)
            return;

        nearbyAnts.Add(ant);
        ant.nearbyInteractables.Add(this);
    }

    private void OnTriggerExit(Collider other)
    {
        var ant = other.GetComponent<Ant>();
        if (!ant)
            return;

        nearbyAnts.Remove(ant);
        ant.nearbyInteractables.Remove(this);
    }

    private void OnDestroy()
    {
        foreach (Ant nearbyAnt in nearbyAnts)
        {
            if (nearbyAnt.currentInteractable == this)
            {
                nearbyAnt.ReleaseInteract(this);
            }
            nearbyAnt.nearbyInteractables.Remove(this);
        }
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
            float dist = (ant.transform.position - possiblePlace.Key).sqrMagnitude;
            if (dist < bestDist)
            {
                closest = possiblePlace.Key;
                bestDist = dist;
            }
        }

        if (closest == Vector3.zero)
        {
            // Uh, panic.
            MysticLog.LogWarning("cant do it.");
            return Vector3.zero;
        }

        places[closest] = ant;

        return closest;
    }

    protected void UnassignFromPoint(Dictionary<Vector3, Ant> places, Ant ant)
    {
        Vector3 place = places.First(p => p.Value == ant).Key;
        places[place] = null;
    }
    
    
    protected float GetBestRadius()
    {
        float scaleFactor = Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
        
        var sphere = GetComponents<SphereCollider>().FirstOrDefault(s => !s.isTrigger);
        if (sphere)
        {
            return sphere.radius * scaleFactor + Ant.RadBuffer;
        }

        var capsule = GetComponents<CapsuleCollider>().FirstOrDefault(s => !s.isTrigger);
        if (capsule)
        {
            return Mathf.Max(capsule.radius, capsule.height) * scaleFactor + Ant.RadBuffer;
        }

        var box = GetComponents<BoxCollider>().FirstOrDefault(s => !s.isTrigger);
        if (box)
        {
            float w = box.size.x;
            float h = box.size.z;
            return Mathf.Sqrt(w * w + h * h) * 0.5f * scaleFactor + Ant.RadBuffer;
        }

        MysticLog.LogWarning("GetBestRadiusFailed!");
        return 0;
    }
}