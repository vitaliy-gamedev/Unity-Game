using UnityEngine;
using UnityEngine.Audio;

namespace GameFoundation.Core
{
    public class AudioService : MonoBehaviour, IAudioService
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string masterParam = "MasterVolume";
        [SerializeField] private string musicParam = "MusicVolume";
        [SerializeField] private string sfxParam = "SFXVolume";

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("UI Clips")]
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip uiHoverClip;

        [Header("Music")]
        [SerializeField] private AudioClip startupMusicClip;

        private const string MasterKey = "gf_audio_master";
        private const string MusicKey = "gf_audio_music";
        private const string SfxKey = "gf_audio_sfx";
        private const float MinDb = -80f;

        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }

        private void Awake()
        {
            GFLogger.RequireField(musicSource, nameof(AudioService), nameof(musicSource));
            GFLogger.RequireField(sfxSource, nameof(AudioService), nameof(sfxSource));

            if (mixer == null)
                GFLogger.Warn(nameof(AudioService), "No AudioMixer assigned.");

            MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicKey, 0.8f);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
        }

        private void Start()
        {
            ApplyAllVolumes();

            if (startupMusicClip != null)
                PlayMusic(startupMusicClip);
        }

        public void PlayUIClick()
        {
            if (uiClickClip != null && sfxSource != null)
                sfxSource.PlayOneShot(uiClickClip);
        }

        public void PlayUIHover()
        {
            if (uiHoverClip != null && sfxSource != null)
                sfxSource.PlayOneShot(uiHoverClip, 0.5f);
        }

        public void SetMasterVolume(float value01)
        {
            MasterVolume = Mathf.Clamp01(value01);
            ApplyToMixer(masterParam, MasterVolume);
            PlayerPrefs.SetFloat(MasterKey, MasterVolume);
        }

        public void SetMusicVolume(float value01)
        {
            MusicVolume = Mathf.Clamp01(value01);
            ApplyToMixer(musicParam, MusicVolume);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        }

        public void SetSfxVolume(float value01)
        {
            SfxVolume = Mathf.Clamp01(value01);
            ApplyToMixer(sfxParam, SfxVolume);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource == null || clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        private void ApplyAllVolumes()
        {
            ApplyToMixer(masterParam, MasterVolume);
            ApplyToMixer(musicParam, MusicVolume);
            ApplyToMixer(sfxParam, SfxVolume);
        }

        private void ApplyToMixer(string param, float value01)
        {
            if (mixer == null || string.IsNullOrEmpty(param)) return;

            float db = value01 <= 0.0001f ? MinDb : Mathf.Log10(value01) * 20f;
            mixer.SetFloat(param, db);
        }
    }
}
