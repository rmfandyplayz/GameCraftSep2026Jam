
using UnityEngine;

public abstract class AntCarriableObject : MonoBehaviour
{
    public abstract void OnPickup();
    public abstract void OnDeposit(AntNest nest);
}