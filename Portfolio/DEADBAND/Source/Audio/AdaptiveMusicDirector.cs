using Deadband.AI;
using Deadband.Combat;
using Deadband.Missions;
using Deadband.Signals;
using UnityEngine;

namespace Deadband.AudioSystem
{
    public enum AdaptiveMusicState
    {
        Exploration,
        SignalLow,
        SignalHigh,
        Combat,
        Extraction
    }

    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class AdaptiveMusicDirector : MonoBehaviour
    {
        [SerializeField] private SignalSystem signalSystem;
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private AudioSource sourceA;
        [SerializeField] private AudioSource sourceB;
        [SerializeField] private AudioClip explorationClip;
        [SerializeField] private AudioClip signalLowClip;
        [SerializeField] private AudioClip signalHighClip;
        [SerializeField] private AudioClip combatClip;
        [SerializeField] private AudioClip extractionClip;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.52f;
        [SerializeField, Min(0.1f)] private float crossfadeSeconds = 2.2f;
        [SerializeField, Min(0.5f)] private float combatHoldSeconds = 5.5f;

        private AudioSource activeSource;
        private AudioSource fadingSource;
        private HumanoidEnemyController[] humanoidEnemies;
        private MeleeEnemyController[] meleeEnemies;
        private SecurityTurretController[] securityTurrets;
        private float combatUntil;
        private float nextCombatScan;
        private float fadeProgress = 1f;

        public AdaptiveMusicState CurrentState { get; private set; } = AdaptiveMusicState.Exploration;
        public string ActiveClipName => activeSource != null && activeSource.clip != null
            ? activeSource.clip.name
            : string.Empty;
        public bool HasCompleteLibrary => sourceA != null && sourceB != null &&
                                          explorationClip != null && signalLowClip != null &&
                                          signalHighClip != null && combatClip != null && extractionClip != null;

        public void Configure(
            SignalSystem signal,
            MissionManager mission,
            AudioSource firstSource,
            AudioSource secondSource,
            AudioClip exploration,
            AudioClip signalLow,
            AudioClip signalHigh,
            AudioClip combat,
            AudioClip extraction)
        {
            signalSystem = signal;
            missionManager = mission;
            sourceA = firstSource;
            sourceB = secondSource;
            explorationClip = exploration;
            signalLowClip = signalLow;
            signalHighClip = signalHigh;
            combatClip = combat;
            extractionClip = extraction;
        }

        private void Awake()
        {
            ConfigureSource(sourceA);
            ConfigureSource(sourceB);
            humanoidEnemies = FindObjectsByType<HumanoidEnemyController>(FindObjectsSortMode.None);
            meleeEnemies = FindObjectsByType<MeleeEnemyController>(FindObjectsSortMode.None);
            securityTurrets = FindObjectsByType<SecurityTurretController>(FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            CombatNoiseSystem.NoiseEmitted += HandleCombatNoise;
            if (signalSystem != null) signalSystem.SignalStrengthChanged += HandleSignalStrengthChanged;
        }

        private void Start()
        {
            CurrentState = EvaluateState();
            activeSource = sourceA;
            PlayClip(activeSource, GetClip(CurrentState), masterVolume);
            Debug.Log("Adaptive music state: " + CurrentState);
            if (sourceB != null)
            {
                sourceB.volume = 0f;
            }
        }

        private void OnDisable()
        {
            CombatNoiseSystem.NoiseEmitted -= HandleCombatNoise;
            if (signalSystem != null) signalSystem.SignalStrengthChanged -= HandleSignalStrengthChanged;
        }

        private void Update()
        {
            ScanCombatState();
            AdaptiveMusicState desiredState = EvaluateState();
            if (desiredState != CurrentState)
            {
                TransitionTo(desiredState);
            }

            UpdateCrossfade();
        }

        private void HandleCombatNoise(CombatNoiseEvent noiseEvent)
        {
            if (noiseEvent.Source != null)
            {
                combatUntil = Mathf.Max(combatUntil, Time.time + combatHoldSeconds);
            }
        }

        private void ScanCombatState()
        {
            if (Time.time < nextCombatScan)
            {
                return;
            }

            nextCombatScan = Time.time + 0.35f;
            foreach (HumanoidEnemyController enemy in humanoidEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy &&
                    enemy.State is EnemyState.Combat or EnemyState.Chase)
                {
                    combatUntil = Mathf.Max(combatUntil, Time.time + combatHoldSeconds);
                    return;
                }
            }

            foreach (SecurityTurretController turret in securityTurrets)
            {
                if (turret != null && turret.gameObject.activeInHierarchy &&
                    turret.State == SecurityState.Firing)
                {
                    combatUntil = Mathf.Max(combatUntil, Time.time + combatHoldSeconds);
                    return;
                }
            }

            foreach (MeleeEnemyController enemy in meleeEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy && enemy.IsInCombat)
                {
                    combatUntil = Mathf.Max(combatUntil, Time.time + combatHoldSeconds);
                    return;
                }
            }
        }

        private void HandleSignalStrengthChanged(float _)
        {
            if (Time.time < combatUntil) return;
            AdaptiveMusicState desiredState = EvaluateState();
            if (desiredState != CurrentState) TransitionTo(desiredState);
        }

        internal void ClearCombatHoldForDiagnostics()
        {
            combatUntil = 0f;
        }

        private AdaptiveMusicState EvaluateState()
        {
            if (missionManager != null && missionManager.State == MissionState.Extracting)
            {
                return AdaptiveMusicState.Extraction;
            }

            if (Time.time < combatUntil)
            {
                return AdaptiveMusicState.Combat;
            }

            float strength = signalSystem != null ? signalSystem.Strength : 0f;
            return EvaluateSignalState(strength);
        }

        public static AdaptiveMusicState EvaluateSignalState(float strength)
        {
            if (strength >= 0.5f)
            {
                return AdaptiveMusicState.SignalHigh;
            }
            if (strength >= 0.25f)
            {
                return AdaptiveMusicState.SignalLow;
            }
            return AdaptiveMusicState.Exploration;
        }

        private void TransitionTo(AdaptiveMusicState state)
        {
            CurrentState = state;
            Debug.Log("Adaptive music state: " + CurrentState);
            AudioClip nextClip = GetClip(state);
            if (nextClip == null || sourceA == null || sourceB == null)
            {
                return;
            }

            fadingSource = activeSource;
            activeSource = activeSource == sourceA ? sourceB : sourceA;
            PlayClip(activeSource, nextClip, 0f);
            fadeProgress = 0f;
        }

        private void UpdateCrossfade()
        {
            if (activeSource == null || fadeProgress >= 1f)
            {
                return;
            }

            fadeProgress = Mathf.MoveTowards(fadeProgress, 1f, Time.unscaledDeltaTime / crossfadeSeconds);
            float eased = fadeProgress * fadeProgress * (3f - 2f * fadeProgress);
            activeSource.volume = eased * masterVolume;
            if (fadingSource != null)
            {
                fadingSource.volume = (1f - eased) * masterVolume;
                if (fadeProgress >= 1f)
                {
                    fadingSource.Stop();
                    fadingSource.clip = null;
                }
            }
        }

        private AudioClip GetClip(AdaptiveMusicState state)
        {
            return state switch
            {
                AdaptiveMusicState.SignalLow => signalLowClip,
                AdaptiveMusicState.SignalHigh => signalHighClip,
                AdaptiveMusicState.Combat => combatClip,
                AdaptiveMusicState.Extraction => extractionClip,
                _ => explorationClip
            };
        }

        private static void ConfigureSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }

        private static void PlayClip(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.clip = clip;
            source.volume = volume;
            source.Play();
        }
    }
}
