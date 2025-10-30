using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Singleton: Easy access from anywhere
    public static AudioManager instance;

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume (0-1)")]
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Persists between scenes
            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSources()
    {
        // Create AudioSources automatically
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true; // Music loops forever
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    // Play Background Music
    public void PlayMusic(AudioClip musicClip)
    {
        musicSource.clip = musicClip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    // Stop Music
    public void StopMusic()
    {
        musicSource.Stop();
    }

    // Play Sound Effect (overlaps OK)
    public void PlaySFX(AudioClip sfxClip)
    {
        sfxSource.PlayOneShot(sfxClip, sfxVolume);
    }

    // Update Volumes (call from UI sliders)
    public void UpdateMusicVolume(float volume)
    {
        musicVolume = volume;
        musicSource.volume = volume;
    }

    public void UpdateSFXVolume(float volume)
    {
        sfxVolume = volume;
    }
}