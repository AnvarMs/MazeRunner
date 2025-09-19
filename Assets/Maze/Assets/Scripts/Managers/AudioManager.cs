using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    public float volume = 1f;
    public bool loop;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public List<Sound> sounds;
    private Dictionary<string, AudioSource> sources = new Dictionary<string, AudioSource>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        foreach (var s in sounds)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = s.clip;
            src.volume = s.volume;
            src.loop = s.loop;
            sources[s.name] = src;
        }
    }

    public void Play(string name)
    {
        if (sources.ContainsKey(name))
            sources[name].Play();
    }

    public void Stop(string name)
    {
        if (sources.ContainsKey(name))
            sources[name].Stop();
    }
}
