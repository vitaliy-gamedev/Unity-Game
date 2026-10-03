using System;
using UnityEngine;

namespace Deadband.Signals
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SignalAudioReaction : MonoBehaviour
    {
        [SerializeField] private SignalSystem signalSystem;
        [SerializeField, Range(0f, 0.2f)] private float maximumVolume = 0.065f;
        [SerializeField, Min(0.1f)] private float responseSpeed = 2.5f;

        private AudioSource audioSource;
        private float targetVolume;
        private float surgeBoost;

        public void Configure(SignalSystem system)
        {
            signalSystem = system;
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.clip = CreateRadioNoise();
            audioSource.Play();
        }

        private void OnEnable()
        {
            if (signalSystem != null)
            {
                signalSystem.SignalStrengthChanged += HandleStrengthChanged;
                signalSystem.SignalSurge += HandleSurge;
            }
        }

        private void Start()
        {
            HandleStrengthChanged(signalSystem != null ? signalSystem.Strength : 0f);
        }

        private void OnDisable()
        {
            if (signalSystem != null)
            {
                signalSystem.SignalStrengthChanged -= HandleStrengthChanged;
                signalSystem.SignalSurge -= HandleSurge;
            }
        }

        private void Update()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.volume = Mathf.MoveTowards(
                audioSource.volume,
                Mathf.Clamp01(targetVolume + surgeBoost * maximumVolume),
                responseSpeed * maximumVolume * Time.deltaTime);
            surgeBoost = Mathf.MoveTowards(surgeBoost, 0f, Time.deltaTime * 1.4f);
            audioSource.pitch = Mathf.Lerp(0.88f, 1.18f, signalSystem != null ? signalSystem.Strength : 0f) + surgeBoost * 0.22f;
        }

        private void HandleStrengthChanged(float strength)
        {
            float audibleSignal = Mathf.InverseLerp(0.18f, 1f, strength);
            targetVolume = audibleSignal * maximumVolume;
        }

        private void HandleSurge(float intensity)
        {
            surgeBoost = Mathf.Max(surgeBoost, Mathf.Clamp01(intensity));
        }

        private static AudioClip CreateRadioNoise()
        {
            const int sampleRate = 22050;
            const int sampleCount = sampleRate * 2;
            float[] samples = new float[sampleCount];
            var random = new System.Random(1701);
            float filteredNoise = 0f;
            for (int index = 0; index < samples.Length; index++)
            {
                float whiteNoise = (float)(random.NextDouble() * 2.0 - 1.0);
                filteredNoise = Mathf.Lerp(filteredNoise, whiteNoise, 0.18f);
                float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(index / (float)sampleRate * Mathf.PI * 3f)), 8f);
                samples[index] = (filteredNoise * 0.6f + pulse * 0.16f) * 0.22f;
            }

            AudioClip clip = AudioClip.Create("Procedural Signal Static", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
