using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AntNest : MonoBehaviour
{
    private static readonly int BirthAnim = Animator.StringToHash("Birth");
    [NonSerialized] public float foodCount;

    private List<Ant> ants = new();
    private List<StoredObject> inventory = new();

    [SerializeField] private Vector2 SpawnArea;

    [SerializeField] private GameObject antPrefab;

    [SerializeField] private Animator Animator;
    [SerializeField] private MusicMan music;
    
    void Awake()
    {
        music = FindAnyObjectByType<MusicMan>();
    }

    private int pendingAntSpawnCount = 0;

    public void CueAntsToSpawn(int count)
    {
        pendingAntSpawnCount += count;
        Animator.SetTrigger(BirthAnim);
    }

    public void SpawnAnts()
    {

        for (; pendingAntSpawnCount > 0; pendingAntSpawnCount--)
        {
            Vector3 pos = transform.position;
            pos.x += Random.Range(-SpawnArea.x, SpawnArea.x);
            pos.z += Random.Range(-SpawnArea.y, SpawnArea.y);

            Vector3 rot = transform.rotation.eulerAngles;
            rot.y = Random.Range(0, 360);
            Instantiate(antPrefab, pos, Quaternion.Euler(rot));
        }
    }

    public void AddAnt(Ant ant)
    {
        ants.Add(ant);
        music.UpdateTargetLayerCount(ants.Count);
    }

    public void RemoveAnt(Ant ant)
    {
        ants.Remove(ant);
    }

    public IEnumerable<Ant> GetAnts()
    {
        return ants;
    }

    public int GetAntCount()
    {
        return ants.Count;
    }

    public void AddToInventory(StoredObject obj)
    {
        inventory.Add(obj);
    }

    public void RemoveFromInventory(StoredObject obj)
    {
        inventory.Remove(obj);
    }

    public float GetMaxHunger()
    {
        return foodCount + 20;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector3(SpawnArea.x*2, 5, SpawnArea.y*2));
    }
}
