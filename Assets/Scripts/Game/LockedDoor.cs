using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    [SerializeField]
    private AudioClip jingle;

    public void Unlock()
    {
        var musicMan = FindAnyObjectByType<MusicMan>();
        if (musicMan != null)
        {
            musicMan.PlaySFX(jingle);
        }

        Destroy(this.gameObject);
    }
}
