using UnityEngine;

public class QueenAnimation : MonoBehaviour
{
    public AntNest nest;

    public AudioClip birthClip;
    public AudioClip eatClip;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void BirthEvent()
    {
        nest.SpawnAnts();

        audioSource.clip = birthClip;
        audioSource.Play();
    }

    public void EatEvent()
    {
        audioSource.clip = eatClip;
        audioSource.Play();
    }
}
