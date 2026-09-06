using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    private AudioSource jingle;

    void Start()
    {
        jingle = GetComponent<AudioSource>();
    }
    public void Unlock()
    {
        jingle.Play();
        Destroy(this.gameObject);
    }
}
