using UnityEngine;

namespace Factura
{
    // A bounded voice pool for short one-shots; music has a separate, quiet looping source.
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private TurretController turret;
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private Health carHealth;
        [SerializeField] private ExplosiveBarrel barrel;
        [SerializeField] private AudioSource music;
        [SerializeField] private AudioClip[] shots;
        [SerializeField] private AudioClip impact;
        [SerializeField] private AudioClip carDamage;
        [SerializeField] private AudioClip explosion;
        [SerializeField, Range(0, 1)] private float effectsVolume = 0.6f;
        [SerializeField, Range(0, 1)] private float musicVolume = 0.18f;
        [SerializeField, Range(1, 12)] private int voiceCount = 6;
        private AudioSource[] voices;
        private int cursor;
        private int shotIndex;
        private float nextImpact;
        private float nextCarHit;
        private float duckUntil;
        private bool focused = true;
        private bool muted;
        private Camera view;

        public int PlayedShots { get; private set; }
        public int PlayedImpacts { get; private set; }
        public int PlayedExplosions { get; private set; }
        public int PlayedCarHits { get; private set; }
        public int VoiceCount => voices?.Length ?? 0;
        public bool IsMuted => muted;
        public float MusicLevel => music.volume;
        public bool HasMusicLoop => music.clip && music.loop;

        private void Awake()
        {
            view = Camera.main;
            voices = new AudioSource[voiceCount];
            for (int i = 0; i < voices.Length; i++)
            {
                var voice = new GameObject($"SFX voice {i + 1:00}");
                voice.transform.SetParent(transform, false);
                voices[i] = voice.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0;
                voices[i].priority = 64;
                voices[i].dopplerLevel = 0;
            }
            music.volume = 0;
        }

        private void OnEnable()
        {
            turret.Fired += OnShot;
            projectiles.Impact += OnImpact;
            carHealth.Damaged += OnCarHit;
            barrel.Exploded += OnExplosion;
            game.StateChanged += OnState;
        }

        private void OnDisable()
        {
            turret.Fired -= OnShot;
            projectiles.Impact -= OnImpact;
            carHealth.Damaged -= OnCarHit;
            barrel.Exploded -= OnExplosion;
            game.StateChanged -= OnState;
            StopVoices();
            music.Stop();
        }

        private void Update()
        {
            float target = !focused || muted ? 0 : musicVolume * (game.State == GameState.Playing ? 1 : 0.35f);
            if (Time.unscaledTime < duckUntil) target *= 0.72f;
            music.volume = Mathf.MoveTowards(music.volume, target, Time.unscaledDeltaTime * 0.3f);
        }

        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value) { StopVoices(); music.Pause(); }
            else if (!muted) ResumeMusic();
        }

        public void SetMuted(bool value)
        {
            muted = value;
            if (value) { StopVoices(); music.Pause(); music.volume = 0; }
            else if (focused) ResumeMusic();
        }

        private void ResumeMusic()
        {
            music.UnPause();
            if (music.clip && !music.isPlaying) music.Play();
        }

        private void OnState(GameState state)
        {
            if (state != GameState.Ready) return;
            StopVoices();
            PlayedShots = PlayedImpacts = PlayedExplosions = PlayedCarHits = 0;
            cursor = shotIndex = 0;
            nextImpact = nextCarHit = duckUntil = 0;
            music.Stop();
            if (focused && !muted && music.clip && !game.IsPreparing) music.Play();
        }

        private void OnShot()
        {
            if (!turret.IsActive || shots.Length == 0) return;
            if (Play(shots[shotIndex++ % shots.Length], 0.32f, turret.Muzzle.position, 1)) PlayedShots++;
            duckUntil = Time.unscaledTime + 0.12f;
        }

        private void OnImpact(Vector3 point, Vector3 normal)
        {
            if (game.State != GameState.Playing || Time.unscaledTime < nextImpact) return;
            nextImpact = Time.unscaledTime + 0.035f;
            if (Play(impact, 0.24f, point, 1.03f - 0.06f * (cursor % 3))) PlayedImpacts++;
        }

        private void OnCarHit(DamageHit hit)
        {
            if (Time.unscaledTime < nextCarHit) return;
            nextCarHit = Time.unscaledTime + 0.1f;
            if (Play(carDamage, 0.38f, hit.Point, 1)) PlayedCarHits++;
            duckUntil = Time.unscaledTime + 0.2f;
        }

        private void OnExplosion(Vector3 point)
        {
            if (Play(explosion, 0.48f, point, 1)) PlayedExplosions++;
            duckUntil = Time.unscaledTime + 0.4f;
        }

        private bool Play(AudioClip clip, float gain, Vector3 point, float pitch)
        {
            if (!clip || muted || !focused) return false;
            int index = cursor;
            for (int i = 0; i < voices.Length; i++)
            {
                int candidate = (cursor + i) % voices.Length;
                if (voices[candidate].isPlaying) continue;
                index = candidate;
                break;
            }
            var source = voices[index];
            cursor = (index + 1) % voices.Length;
            source.Stop();
            source.clip = clip;
            float distance = Vector3.Distance(turret.transform.position, point);
            source.volume = gain * effectsVolume * Mathf.Lerp(1, 0.45f, Mathf.Clamp01(distance / 35));
            source.pitch = pitch;
            source.panStereo = view ? Mathf.Clamp((view.WorldToViewportPoint(point).x - 0.5f) * 0.6f, -0.35f, 0.35f) : 0;
            source.Play();
            return true;
        }

        private void StopVoices()
        {
            if (voices == null) return;
            foreach (var voice in voices) if (voice) { voice.Stop(); voice.clip = null; }
        }
    }
}
