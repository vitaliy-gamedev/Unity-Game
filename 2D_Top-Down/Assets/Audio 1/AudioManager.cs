using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private AudioMixer _audioMixer;

    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Audio Clips")] [SerializeField]
    private AudioClip _mainMenuMusic;

    [SerializeField] private AudioClip _gameplayMusic;
    [SerializeField] private AudioClip _sfxButtonClick;
    [SerializeField] private AudioClip _sfxItemPickup;
    [SerializeField] private AudioClip _sfxPlayerHit;
    [SerializeField] private AudioClip _sfxEnemyDeath;
    [SerializeField] private AudioClip _sfxLevelComplete;
    [SerializeField] private AudioClip _sfxGameOver;

    private static AudioManager _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ApplyVolumeSettings();
    }

    public void PlayMainMenuMusic()
    {
        PlayMusic(_mainMenuMusic);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(_gameplayMusic);
    }

    public void StopMusic()
    {
        _musicSource.Stop();
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        if (_musicSource.isPlaying && _musicSource.clip == clip) return;

        _musicSource.clip = clip;
        _musicSource.loop = true;
        _musicSource.Play();
    }

    public void PlayButtonClick() => PlaySFX(_sfxButtonClick);
    public void PlayItemPickup() => PlaySFX(_sfxItemPickup);
    public void PlayPlayerHit() => PlaySFX(_sfxPlayerHit);
    public void PlayEnemyDeath() => PlaySFX(_sfxEnemyDeath);
    public void PlayLevelComplete() => PlaySFX(_sfxLevelComplete);
    public void PlayGameOver() => PlaySFX(_sfxGameOver);

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volumeScale);
    }

    public void ApplyVolumeSettings()
    {
        if (_audioMixer == null) return;

        _audioMixer.SetFloat("MasterVolume", Mathf.Log10(SettingsManager.GetMasterVolume()) * 20);
        _audioMixer.SetFloat("SFXVolume", Mathf.Log10(SettingsManager.GetSFXVolume()) * 20);
        _audioMixer.SetFloat("MusicVolume", Mathf.Log10(SettingsManager.GetMusicVolume()) * 20);
    }

    public static AudioManager Instance => _instance;
}