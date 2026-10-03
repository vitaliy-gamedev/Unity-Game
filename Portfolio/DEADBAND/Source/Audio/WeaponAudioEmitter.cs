using UnityEngine;

namespace Deadband.AudioSystem
{
    [DisallowMultipleComponent]
    public sealed class WeaponAudioEmitter : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shotClip;
        [SerializeField] private AudioClip dryFireClip;
        [SerializeField] private bool playerControlled;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 0.7f;
        [SerializeField, Range(0.5f, 1.5f)] private float pitchScale = 1f;

        public bool IsConfigured => audioSource != null && shotClip != null && dryFireClip != null;
        public AudioSource Source => audioSource;
        public int ShotPlayCount { get; private set; }
        public int DryFirePlayCount { get; private set; }
        public AudioClip ActiveClip => audioSource != null ? audioSource.clip : null;

        public void Configure(
            AudioSource source,
            AudioClip rifleShot,
            AudioClip emptyWeapon,
            bool isPlayer,
            float volume,
            float pitch = 1f)
        {
            audioSource = source;
            shotClip = rifleShot;
            dryFireClip = emptyWeapon;
            playerControlled = isPlayer;
            volumeScale = Mathf.Clamp01(volume);
            pitchScale = Mathf.Clamp(pitch, 0.5f, 1.5f);
            ConfigureAudioSource();
        }

        private void Awake()
        {
            ConfigureAudioSource();
        }

        public void PlayShot(bool pistol)
        {
            if (audioSource == null || shotClip == null)
            {
                return;
            }

            audioSource.Stop();
            audioSource.clip = shotClip;
            audioSource.volume = volumeScale;
            audioSource.pitch = pitchScale * (pistol ? 1.17f : 1f) * Random.Range(0.97f, 1.035f);
            audioSource.Play();
            ShotPlayCount++;
        }

        public void PlayDryFire()
        {
            if (audioSource == null || dryFireClip == null)
            {
                return;
            }

            audioSource.Stop();
            audioSource.clip = dryFireClip;
            audioSource.volume = volumeScale * 0.62f;
            audioSource.pitch = pitchScale * Random.Range(0.96f, 1.04f);
            audioSource.Play();
            DryFirePlayCount++;
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = playerControlled ? 0.2f : 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 72f;
            audioSource.dopplerLevel = 0f;
        }
    }
}
