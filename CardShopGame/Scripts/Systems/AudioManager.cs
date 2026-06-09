using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple string-keyed audio manager.
/// Add AudioClipEntry items in the Inspector to register sounds.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    [System.Serializable]
    public struct AudioClipEntry
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    [SerializeField] private List<AudioClipEntry> _clips;
    private Dictionary<string, AudioClipEntry> _map;
    private AudioSource _source;

    private void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _source = gameObject.AddComponent<AudioSource>();
        _map    = new Dictionary<string, AudioClipEntry>();
        foreach (var entry in _clips)
            _map[entry.key] = entry;
    }

    /// <summary>Play a one-shot sound by key.</summary>
    public static void Play(string key)
    {
        if (_instance == null || !_instance._map.TryGetValue(key, out var entry)) return;
        _instance._source.PlayOneShot(entry.clip, entry.volume);
    }

    /// <summary>Play background music (loops).</summary>
    public static void PlayMusic(string key)
    {
        if (_instance == null || !_instance._map.TryGetValue(key, out var entry)) return;
        _instance._source.clip   = entry.clip;
        _instance._source.loop   = true;
        _instance._source.volume = entry.volume;
        _instance._source.Play();
    }

    public static void StopMusic() => _instance?._source.Stop();
}
