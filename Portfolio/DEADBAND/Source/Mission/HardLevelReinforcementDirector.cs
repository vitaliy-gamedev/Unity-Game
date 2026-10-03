using Deadband.Combat;
using Deadband.Signals;
using Deadband.Squad;
using UnityEngine;
using UnityEngine.AI;

namespace Deadband.Missions
{
    [DisallowMultipleComponent]
    public sealed class HardLevelReinforcementDirector : MonoBehaviour
    {
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private SignalSystem signalSystem;
        [SerializeField] private GameObject[] reinforcementWaves;
        [SerializeField] private float[] signalThresholds = { 0.2f, 0.42f, 0.64f };
        [SerializeField, Min(10f)] private float firstTimedWaveDelay = 38f;
        [SerializeField, Min(10f)] private float timedWaveInterval = 48f;

        private int nextWaveIndex;
        private float nextTimedWaveAt;
        private float alertTimeRemaining;
        private string alertText;

        public int SpawnedWaveCount => nextWaveIndex;
        public int TotalWaveCount => reinforcementWaves?.Length ?? 0;

        public void Configure(
            MissionManager manager,
            SignalSystem signal,
            GameObject[] waves,
            float[] thresholds,
            float firstDelay,
            float interval)
        {
            missionManager = manager;
            signalSystem = signal;
            reinforcementWaves = waves;
            signalThresholds = thresholds;
            firstTimedWaveDelay = Mathf.Max(10f, firstDelay);
            timedWaveInterval = Mathf.Max(10f, interval);
        }

        private void Awake()
        {
            SetAllWavesInactive();
        }

        private void Start()
        {
            nextTimedWaveAt = Time.time + firstTimedWaveDelay;
            if (signalSystem != null) signalSystem.SignalStrengthChanged += HandleSignalStrengthChanged;
            if (missionManager != null) missionManager.StateChanged += HandleMissionStateChanged;
        }

        private void OnDestroy()
        {
            if (signalSystem != null) signalSystem.SignalStrengthChanged -= HandleSignalStrengthChanged;
            if (missionManager != null) missionManager.StateChanged -= HandleMissionStateChanged;
        }

        private void Update()
        {
            alertTimeRemaining = Mathf.Max(0f, alertTimeRemaining - Time.unscaledDeltaTime);
            if (missionManager == null || missionManager.State is MissionState.Completed or MissionState.Failed ||
                nextWaveIndex >= Mathf.Max(0, TotalWaveCount - 1) || Time.time < nextTimedWaveAt)
            {
                return;
            }

            SpawnNextWave("MOVEMENT DETECTED ON THE PERIMETER");
        }

        private void HandleSignalStrengthChanged(float strength)
        {
            while (nextWaveIndex < Mathf.Min(signalThresholds?.Length ?? 0, Mathf.Max(0, TotalWaveCount - 1)) &&
                   strength >= signalThresholds[nextWaveIndex])
            {
                SpawnNextWave("SIGNAL HAS DRAWN IN MORE INFECTED");
            }
        }

        private void HandleMissionStateChanged(MissionState state)
        {
            if (state != MissionState.Extracting) return;
            while (nextWaveIndex < TotalWaveCount)
            {
                SpawnNextWave("EXTRACTION CONTACT  //  FINAL WAVE INBOUND");
            }
        }

        public void ForceNextWaveForDiagnostics()
        {
            SpawnNextWave("DIAGNOSTIC REINFORCEMENT WAVE");
        }

        private void SpawnNextWave(string message)
        {
            if (nextWaveIndex >= TotalWaveCount) return;
            GameObject wave = reinforcementWaves[nextWaveIndex];
            nextWaveIndex++;
            nextTimedWaveAt = Time.time + timedWaveInterval;
            if (wave == null) return;

            foreach (NavMeshAgent agent in wave.GetComponentsInChildren<NavMeshAgent>(true))
            {
                NavMeshRuntimeBootstrap.SnapAgentToNavMesh(agent, 18f, true);
            }
            wave.SetActive(true);
            alertText = $"REINFORCEMENTS {nextWaveIndex}/{TotalWaveCount}  //  {message}";
            alertTimeRemaining = 3f;
            CombatNoiseSystem.Emit(wave.transform.position, 52f, gameObject);
        }

        private void SetAllWavesInactive()
        {
            if (reinforcementWaves == null) return;
            foreach (GameObject wave in reinforcementWaves)
            {
                if (wave != null) wave.SetActive(false);
            }
        }

        private void OnGUI()
        {
            // Reinforcements are announced by their beacon, music and enemy audio.
        }
    }
}
