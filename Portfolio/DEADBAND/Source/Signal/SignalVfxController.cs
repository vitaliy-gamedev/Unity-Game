using UnityEngine;

namespace Deadband.Signals
{
    [DisallowMultipleComponent]
    public sealed class SignalVfxController : MonoBehaviour
    {
        [SerializeField] private SignalSystem signalSystem;
        [SerializeField] private ParticleSystem coreParticles;
        [SerializeField] private Light coreLight;
        [SerializeField, Min(0f)] private float normalFogDensity = 0.0034f;
        [SerializeField, Min(0f)] private float coreFogDensity = 0.0068f;

        private float targetStrength;
        private float smoothedStrength;
        private float surgeRemaining;

        public void Configure(SignalSystem system, ParticleSystem particles, Light lightReference)
        {
            signalSystem = system;
            coreParticles = particles;
            coreLight = lightReference;
        }

        public void ConfigureFog(float normalDensity, float coreDensity)
        {
            normalFogDensity = Mathf.Max(0f, normalDensity);
            coreFogDensity = Mathf.Max(normalFogDensity, coreDensity);
        }

        private void OnEnable()
        {
            if (signalSystem == null) return;
            signalSystem.SignalStrengthChanged += HandleStrengthChanged;
            signalSystem.SignalSurge += HandleSurge;
        }

        private void Start()
        {
            targetStrength = signalSystem != null ? signalSystem.Strength : 0f;
            smoothedStrength = targetStrength;
            if (coreParticles != null && !coreParticles.isPlaying) coreParticles.Play();
        }

        private void OnDisable()
        {
            if (signalSystem != null)
            {
                signalSystem.SignalStrengthChanged -= HandleStrengthChanged;
                signalSystem.SignalSurge -= HandleSurge;
            }
            RenderSettings.fogDensity = normalFogDensity;
        }

        private void Update()
        {
            smoothedStrength = Mathf.MoveTowards(smoothedStrength, targetStrength, Time.deltaTime * 0.35f);
            surgeRemaining = Mathf.Max(0f, surgeRemaining - Time.deltaTime);
            float surgePulse = surgeRemaining > 0f ? 0.5f + Mathf.PingPong(Time.time * 8f, 0.5f) : 0f;
            float visualStrength = Mathf.Clamp01(smoothedStrength + surgePulse * 0.35f);

            RenderSettings.fogDensity = Mathf.Lerp(normalFogDensity, coreFogDensity, visualStrength * visualStrength);
            if (coreParticles != null)
            {
                ParticleSystem.EmissionModule emission = coreParticles.emission;
                emission.rateOverTime = Mathf.Lerp(4f, 30f, visualStrength);
                ParticleSystem.MainModule main = coreParticles.main;
                main.startColor = Color.Lerp(new Color(0.18f, 1f, 0.45f, 0.35f), new Color(1f, 0.08f, 0.5f, 0.9f), visualStrength);
            }
            if (coreLight != null)
            {
                coreLight.intensity = Mathf.Lerp(1.4f, 7f, visualStrength) + surgePulse * 4f;
                coreLight.color = Color.Lerp(new Color(0.2f, 1f, 0.45f), new Color(1f, 0.08f, 0.42f), visualStrength);
            }
        }

        private void HandleStrengthChanged(float strength)
        {
            targetStrength = Mathf.Clamp01(strength);
        }

        private void HandleSurge(float intensity)
        {
            surgeRemaining = Mathf.Max(surgeRemaining, Mathf.Lerp(0.5f, 1.6f, intensity));
        }
    }
}
