using UnityEngine;

public class KeyLargeCarriable : AntLargeCarriableObject
{
    [SerializeField] private LockedDoor connectedDoor;
    protected override void DepositToNest(AntNest nest)
    {
        MysticLog.Log("we made it back");
        connectedDoor.Unlock();
    }

    
}
