using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    public void Unlock()
    {
        Destroy(this.gameObject);
    }
}
