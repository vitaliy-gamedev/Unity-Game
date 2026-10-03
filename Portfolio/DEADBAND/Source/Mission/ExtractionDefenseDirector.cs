using Deadband.Combat;
using UnityEngine;

namespace Deadband.Missions
{
    [DisallowMultipleComponent]
    public sealed class ExtractionDefenseDirector : MonoBehaviour
    {
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private GameObject[] reinforcementGroups;
        private float alertTime;

        public int ReinforcementCount => reinforcementGroups?.Length ?? 0;

        public void Configure(MissionManager manager, GameObject[] reinforcements)
        {
            missionManager = manager;
            reinforcementGroups = reinforcements;
        }

        private void Start()
        {
            SetReinforcementsActive(false);
            if (missionManager != null) missionManager.StateChanged += HandleMissionStateChanged;
        }

        private void OnDestroy()
        {
            if (missionManager != null) missionManager.StateChanged -= HandleMissionStateChanged;
        }

        private void Update()
        {
            alertTime = Mathf.Max(0f, alertTime - Time.unscaledDeltaTime);
        }

        private void HandleMissionStateChanged(MissionState state)
        {
            if (state != MissionState.Extracting) return;
            SetReinforcementsActive(true);
            alertTime = 5f;
            if (missionManager.ExtractionPoint != null)
            {
                CombatNoiseSystem.Emit(missionManager.ExtractionPoint.Position, 55f, gameObject);
            }
        }

        private void SetReinforcementsActive(bool active)
        {
            if (reinforcementGroups == null) return;
            foreach (GameObject group in reinforcementGroups)
            {
                if (group != null) group.SetActive(active);
            }
        }

        private void OnGUI()
        {
            // The active extraction beacon and incoming hostiles provide the warning.
        }
    }
}
