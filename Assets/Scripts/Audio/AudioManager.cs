using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    
    [Header("Music")]
    public AudioClip[] ambientTracks;
    public AudioClip chaseMusic;
    public AudioClip escapeMusic;
    public AudioSource musicSource;
    public float musicCrossfadeTime = 2f;
    
    [Header("Ambience")]
    public AudioSource ambienceSource;
    public AudioClip[] ambientLoops;
    
    [Header("SFX")]
    public AudioSource sfxSource;
    
    [Header("Witch Audio")]
    public AudioSource witchProximitySource;
    public float maxProximityVolume = 0.8f;
    
    private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    private Coroutine musicCrossfadeCoroutine;
    private string currentMusicTrack;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups("Music")[0];
        }
        
        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
            ambienceSource.volume = 0.5f;
            ambienceSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups("Ambience")[0];
        }
        
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups("SFX")[0];
        }
        
        if (witchProximitySource == null)
        {
            witchProximitySource = gameObject.AddComponent<AudioSource>();
            witchProximitySource.loop = true;
            witchProximitySource.playOnAwake = false;
            witchProximitySource.volume = 0f;
            witchProximitySource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups("Witch")[0];
        }
    }
    
    private void Start()
    {
        PlayAmbience();
    }
    
    public void PlayMusic(string trackName, bool crossfade = true)
    {
        if (currentMusicTrack == trackName) return;
        
        AudioClip clip = GetMusicClip(trackName);
        if (clip == null) return;
        
        if (crossfade && musicSource.isPlaying)
        {
            if (musicCrossfadeCoroutine != null)
                StopCoroutine(musicCrossfadeCoroutine);
            musicCrossfadeCoroutine = StartCoroutine(CrossfadeMusic(clip));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
        
        currentMusicTrack = trackName;
    }
    
    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        
        while (elapsed < musicCrossfadeTime)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicCrossfadeTime);
            yield return null;
        }
        
        musicSource.clip = newClip;
        musicSource.Play();
        
        elapsed = 0f;
        while (elapsed < musicCrossfadeTime)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, startVolume, elapsed / musicCrossfadeTime);
            yield return null;
        }
        
        musicSource.volume = startVolume;
    }
    
    private AudioClip GetMusicClip(string trackName)
    {
        switch (trackName)
        {
            case "ambient":
                return ambientTracks.Length > 0 ? ambientTracks[Random.Range(0, ambientTracks.Length)] : null;
            case "chase":
                return chaseMusic;
            case "escape":
                return escapeMusic;
            default:
                return null;
        }
    }
    
    public void PlayAmbience(int index = -1)
    {
        if (ambientLoops.Length == 0) return;
        
        AudioClip clip = index >= 0 && index < ambientLoops.Length ? ambientLoops[index] : ambientLoops[Random.Range(0, ambientLoops.Length)];
        ambienceSource.clip = clip;
        ambienceSource.Play();
    }
    
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }
    
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }
    
    public void SetWitchProximityAudio(AudioClip clip, float intensity)
    {
        if (clip != null)
        {
            witchProximitySource.clip = clip;
            if (!witchProximitySource.isPlaying)
                witchProximitySource.Play();
        }
        
        witchProximitySource.volume = Mathf.Lerp(0f, maxProximityVolume, intensity);
    }
    
    public void StopWitchProximityAudio()
    {
        witchProximitySource.Stop();
        witchProximitySource.volume = 0f;
    }
    
    public void SetMasterVolume(float volume)
    {
        audioMixer?.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f);
    }
    
    public void SetMusicVolume(float volume)
    {
        audioMixer?.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f);
    }
    
    public void SetSFXVolume(float volume)
    {
        audioMixer?.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f);
    }
    
    public void RegisterSFX(string name, AudioClip clip)
    {
        if (!sfxClips.ContainsKey(name))
            sfxClips[name] = clip;
    }
    
    public void PlayRegisteredSFX(string name, float volume = 1f)
    {
        if (sfxClips.TryGetValue(name, out AudioClip clip))
        {
            PlaySFX(clip, volume);
        }
    }
}
