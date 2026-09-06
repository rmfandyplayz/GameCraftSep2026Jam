using UnityEngine;

public class MainMenuMusic : MonoBehaviour
{
    public AudioSource audioSource;
    public MusicMan musicMan;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private bool started;
    private bool audioPlaying = true;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (started && audioPlaying)
        {
            if (audioSource.volume > 0f)
            {
                audioSource.volume -= 0.02f;
            }
            if (audioSource.volume <= 0f)
            {
                audioSource.volume = 0f;
                audioPlaying = false;
                if (musicMan != null)
                    musicMan.mainMenuLeft = true;
            }
        }
    }

    public void FadeOut()
    {
        started = true;
    }

    public void Restart()
    {
    //don't need to use ig
    }
}
