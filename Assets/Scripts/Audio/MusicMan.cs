using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class MusicMan : MonoBehaviour
{
    [Range(0f, 1f)]
    public float volume = 0.5f;

    [Range(0, 13)]
    public int targetLayerCount;

    private int layerCount;

    private int percussionLevel => layerCount / 3;

    public List<AudioSource> activeLayers;
    public List<AudioSource> inactiveLayers;

    private List<MusicTrackSettings> requirements;
    private List<MusicTrackSettings> percRequirements;

    public AudioSource SnareDrum;
    public AudioSource BassDrum;
    public AudioSource BongoDrum;
    public AudioSource Crash;
    public AudioSource Timpani;
    public AudioSource Piccolo;
    public AudioSource Clarinet;
    public AudioSource Oboe;
    public AudioSource Bassoon;
    public AudioSource Trumpet;
    public AudioSource Horn;
    public AudioSource Trombone;
    public AudioSource Tuba;

    public class MusicTrackSettings
    {
        public MusicTrackSettings(AudioSource audioSource, List<AudioSource> requirements, int percussionLevel, bool percussion = false) 
        
        {
            this.audioSource = audioSource;
            this.requirements = requirements;
            this.requiredPercussionLevel = percussionLevel;
            this.percussion = percussion;
        }

        public bool CanBeAdded(MusicMan musicMan)
        {
            if (musicMan.activeLayers.Contains(audioSource))
                return false;
            if (musicMan.percussionLevel < requiredPercussionLevel)
                return false;

            if (requirements.Count == 0)
                return true;

            foreach (var a in musicMan.activeLayers)
            {
                if (requirements.Contains(a))
                    return true;
            }
            return false;
        }

        public bool CanBeRemoved(MusicMan musicMan)
        {
            if (!musicMan.activeLayers.Contains(audioSource))
                return false;

            Dictionary<AudioSource, MusicTrackSettings> hypotheticalList = new();
            foreach (var a in percussion ? musicMan.percRequirements : musicMan.requirements)
            {
                if (musicMan.activeLayers.Contains(a.audioSource))
                {
                    hypotheticalList[a.audioSource] = a;
                }
            }

            return TestForLegality(hypotheticalList);
        }

        public bool TestForLegality(Dictionary<AudioSource, MusicTrackSettings> hypotheticalList)
        {
            hypotheticalList.Remove(audioSource);
            foreach (var a in requirements)
            {
                if (hypotheticalList.TryGetValue(a, out var b) && !b.TestForLegality(hypotheticalList))
                    return false;
            }
            return true;
        }

        public AudioSource audioSource;
        bool percussion;

        List<AudioSource> requirements;
        public int requiredPercussionLevel;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        requirements = new()
        {
        new MusicTrackSettings(Piccolo, new() { SnareDrum }, 0),
        new MusicTrackSettings(Trombone, new() { SnareDrum, Timpani }, 0),
        new MusicTrackSettings(Horn, new() { Piccolo, Trombone }, 0),
        new MusicTrackSettings(Clarinet, new() { Piccolo, Oboe, Bassoon }, 0),


        new MusicTrackSettings(Trumpet, new() { Tuba, Trombone }, 1),
        new MusicTrackSettings(Oboe, new() { Clarinet, Bassoon }, 1),
        new MusicTrackSettings(Bassoon, new() { Clarinet, Oboe, Horn }, 1),

        new MusicTrackSettings(Tuba, new() { Horn, Bassoon, Trumpet }, 2),
            };

        percRequirements = new()
        {
        new MusicTrackSettings(SnareDrum, new(), 0, true),
        new MusicTrackSettings(Timpani, new(), 0, true),
        new MusicTrackSettings(BassDrum, new(), 2, true),
        new MusicTrackSettings(Crash, new(), 2, true),
        new MusicTrackSettings(BongoDrum, new(), 3, true),
        };

        inactiveLayers = new()
        {
            SnareDrum, BassDrum, Timpani, Crash, BongoDrum, Piccolo, Clarinet, Oboe, Bassoon, Trumpet, Horn, Trombone, Tuba
        };
    }

    // Update is called once per frame
    void Update()
    {
        while (layerCount < targetLayerCount)
        {
            AddLayer(layerCount % 3 == 0);
            layerCount++;
        }

        while (layerCount > targetLayerCount)
        {
            layerCount--;
            RemoveLayer(layerCount % 3 == 0);
        }

        foreach (var a in activeLayers)
        {
            if (a.volume < volume)
                a.volume += 0.0025f;
            if (a.volume > volume)
                a.volume = volume;
        }

        foreach (var a in inactiveLayers)
        {
            if (a.volume > 0f)
                a.volume -= 0.0025f;
            if (a.volume < 0f)
                a.volume = 0f;
        }
    }

    private void AddLayer(bool percussion)
    {
        List<AudioSource> potentialAudios = new();

        foreach (var a in percussion ? percRequirements : requirements)
        {
            if (a.CanBeAdded(this))
                potentialAudios.Add(a.audioSource);
        }

        AudioSource source = potentialAudios[Random.Range(0, potentialAudios.Count)];
        activeLayers.Add(source);
        inactiveLayers.Remove(source);
    }

    private void RemoveLayer(bool percussion)
    {
        List<MusicTrackSettings> potentialAudios = new();

        int highestRequiredPercussionLevel = 0;
        foreach (var a in percussion ? percRequirements : requirements)
        {
            if (a.CanBeRemoved(this))
            {
                potentialAudios.Add(a);
                highestRequiredPercussionLevel = Mathf.Max(a.requiredPercussionLevel, highestRequiredPercussionLevel);
            }
        }

        if (percussionLevel < highestRequiredPercussionLevel)
        {
            for (int i = potentialAudios.Count - 1; i >= 0; i--)
            {
                if (potentialAudios[i].requiredPercussionLevel < highestRequiredPercussionLevel)
                {
                    potentialAudios.Remove(potentialAudios[i]);
                }
            }
        }

        AudioSource source = potentialAudios[Random.Range(0, potentialAudios.Count)].audioSource;
        activeLayers.Remove(source);
        inactiveLayers.Add(source);
    }
}
