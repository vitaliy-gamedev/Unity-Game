using Deadband.CameraSystem;
using UnityEngine;

namespace Deadband.Combat
{
    [DefaultExecutionOrder(-550)]
    [DisallowMultipleComponent]
    public sealed class PlayerCombatFeedback : MonoBehaviour
    {
        private const float HitMarkerDuration = 0.17f;
        private const float KillMarkerDuration = 0.32f;

        private Health health;
        private ThirdPersonCameraController cameraController;
        private AudioSource feedbackSource;
        private AudioClip hitClip;
        private AudioClip killClip;
        private AudioClip reloadStartClip;
        private AudioClip reloadCompleteClip;
        private AudioClip warningClip;
        private float previousHealth;
        private float hitMarkerUntil;
        private float damageFlashUntil;
        private float damageFlashStrength;
        private bool lethalHit;

        public bool HitMarkerVisible => Time.unscaledTime < hitMarkerUntil;
        public bool DamageFeedbackVisible => Time.unscaledTime < damageFlashUntil;

        private void Awake()
        {
            health = GetComponent<Health>();
            cameraController = FindFirstObjectByType<ThirdPersonCameraController>();
            feedbackSource = gameObject.AddComponent<AudioSource>();
            feedbackSource.playOnAwake = false;
            feedbackSource.loop = false;
            feedbackSource.spatialBlend = 0f;
            feedbackSource.volume = 0.7f;
            feedbackSource.dopplerLevel = 0f;

            hitClip = CreateTone("Hit Confirm", 920f, 0.055f, 0.2f, 0.015f);
            killClip = CreateTone("Kill Confirm", 620f, 0.12f, 0.18f, 0.03f);
            reloadStartClip = CreateTone("Reload Magazine Out", 230f, 0.075f, 0.08f, 0.055f);
            reloadCompleteClip = CreateTone("Reload Magazine In", 360f, 0.09f, 0.1f, 0.045f);
            warningClip = CreateTone("Weapon Warning", 145f, 0.08f, 0.11f, 0.035f);
        }

        private void OnEnable()
        {
            if (health == null) return;
            previousHealth = health.CurrentHealth;
            health.HealthChanged += HandleHealthChanged;
        }

        private void Start()
        {
            if (health != null) previousHealth = health.CurrentHealth;
        }

        private void OnDisable()
        {
            if (health != null) health.HealthChanged -= HandleHealthChanged;
        }

        private void OnDestroy()
        {
            DestroyClip(hitClip);
            DestroyClip(killClip);
            DestroyClip(reloadStartClip);
            DestroyClip(reloadCompleteClip);
            DestroyClip(warningClip);
        }

        public void NotifyShot(bool pistol)
        {
            ResolveCamera()?.AddRecoil(
                pistol ? 0.48f : 0.72f,
                Random.Range(pistol ? -0.14f : -0.22f, pistol ? 0.14f : 0.22f));
        }

        public void NotifyHit(bool lethal, Vector3 hitPoint)
        {
            lethalHit = lethal;
            hitMarkerUntil = Time.unscaledTime + (lethal ? KillMarkerDuration : HitMarkerDuration);
            PlayFeedback(lethal ? killClip : hitClip, lethal ? 0.72f : 0.52f, lethal ? 0.92f : 1f);
            SpawnImpact(hitPoint, lethal ? new Color(1f, 0.2f, 0.08f) : new Color(1f, 0.82f, 0.32f));
        }

        public void NotifyWorldImpact(Vector3 hitPoint)
        {
            SpawnImpact(hitPoint, new Color(0.72f, 0.82f, 0.74f));
        }

        public void NotifyDryFire()
        {
            PlayFeedback(warningClip, 0.42f, 0.88f);
        }

        public void NotifyReloadStarted(float duration)
        {
            PlayFeedback(reloadStartClip, 0.48f, 0.92f);
        }

        public void NotifyReloadCompleted()
        {
            PlayFeedback(reloadCompleteClip, 0.52f, 1.04f);
        }

        private void HandleHealthChanged(float currentHealth, float maximumHealth)
        {
            if (previousHealth <= 0f && currentHealth > 0f)
            {
                previousHealth = maximumHealth;
            }
            if (currentHealth < previousHealth && currentHealth > 0f)
            {
                float damage = previousHealth - currentHealth;
                damageFlashStrength = Mathf.Clamp01(damage / Mathf.Max(1f, maximumHealth) * 2.8f);
                damageFlashUntil = Time.unscaledTime + Mathf.Lerp(0.18f, 0.42f, damageFlashStrength);
                ResolveCamera()?.AddRecoil(
                    Mathf.Lerp(0.2f, 0.62f, damageFlashStrength),
                    Random.Range(-0.34f, 0.34f));
                PlayFeedback(warningClip, Mathf.Lerp(0.18f, 0.45f, damageFlashStrength), 0.72f);
            }
            previousHealth = currentHealth;
        }

        private ThirdPersonCameraController ResolveCamera()
        {
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<ThirdPersonCameraController>();
            }
            return cameraController;
        }

        private void PlayFeedback(AudioClip clip, float volume, float pitch)
        {
            if (feedbackSource == null || clip == null || AudioListener.pause) return;
            feedbackSource.pitch = pitch;
            feedbackSource.PlayOneShot(clip, volume);
        }

        private void OnGUI()
        {
            DrawDamageFeedback();
            DrawHitMarker();
        }

        private void DrawDamageFeedback()
        {
            if (!DamageFeedbackVisible) return;
            float remaining = Mathf.Clamp01((damageFlashUntil - Time.unscaledTime) / 0.42f);
            float alpha = (0.1f + damageFlashStrength * 0.28f) * remaining;
            float thickness = Mathf.Lerp(24f, 58f, damageFlashStrength);
            Color previous = GUI.color;
            GUI.color = new Color(0.82f, 0.02f, 0.01f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, Screen.height - thickness, Screen.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, thickness, thickness, Screen.height - thickness * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - thickness, thickness, thickness, Screen.height - thickness * 2f), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawHitMarker()
        {
            if (!HitMarkerVisible) return;
            float duration = lethalHit ? KillMarkerDuration : HitMarkerDuration;
            float alpha = Mathf.Clamp01((hitMarkerUntil - Time.unscaledTime) / duration);
            Vector2 center = new(Screen.width * 0.5f, Screen.height * 0.5f);
            Color previous = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.color = lethalHit
                ? new Color(1f, 0.18f, 0.08f, alpha)
                : new Color(1f, 0.94f, 0.72f, alpha);
            GUIUtility.RotateAroundPivot(45f, center);
            GUI.DrawTexture(new Rect(center.x - 1f, center.y - 16f, 2f, 10f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(center.x - 1f, center.y + 6f, 2f, 10f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(center.x - 16f, center.y - 1f, 10f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(center.x + 6f, center.y - 1f, 10f, 2f), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previous;

            if (lethalHit)
            {
                GUIStyle style = new(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1f, 0.38f, 0.2f, alpha) }
                };
                GUI.Label(new Rect(center.x - 80f, center.y + 22f, 160f, 22f), "TARGET ELIMINATED", style);
            }
        }

        private static void SpawnImpact(Vector3 position, Color color)
        {
            GameObject impact = new("Bullet Impact Feedback");
            impact.transform.position = position;
            ParticleSystem particles = impact.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.12f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.075f);
            main.startColor = color;
            main.gravityModifier = 0.2f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 7, 11) });
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.035f;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                Material material = new(shader) { color = color };
                impact.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                Destroy(material, 0.5f);
            }
            particles.Play();
        }

        private static AudioClip CreateTone(
            string clipName,
            float frequency,
            float duration,
            float noiseAmount,
            float attack)
        {
            int sampleRate = Mathf.Max(22050, AudioSettings.outputSampleRate);
            int sampleCount = Mathf.Max(64, Mathf.CeilToInt(duration * sampleRate));
            float[] samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)sampleRate;
                float progress = index / (float)(sampleCount - 1);
                float attackEnvelope = Mathf.Clamp01(time / Mathf.Max(0.001f, attack));
                float envelope = attackEnvelope * Mathf.Pow(1f - progress, 2.2f);
                float tone = Mathf.Sin(time * frequency * Mathf.PI * 2f);
                float noise = Random.Range(-1f, 1f) * noiseAmount;
                samples[index] = (tone * (1f - noiseAmount) + noise) * envelope * 0.55f;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void DestroyClip(AudioClip clip)
        {
            if (clip != null) Destroy(clip);
        }
    }
}
