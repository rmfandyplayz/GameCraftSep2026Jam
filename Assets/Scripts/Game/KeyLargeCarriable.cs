using UnityEngine;

public class KeyLargeCarriable : AntLargeCarriableObject
{
    [SerializeField] private LockedDoor connectedDoor;
    protected override void DepositToNest(AntNest nest)
    {
        connectedDoor.Unlock();
    }

    void Start()
    {
        
    }

    
}
