using UnityEngine;

namespace Deadband.Missions
{
    [CreateAssetMenu(menuName = "DEADBAND/Mission/Mission Data", fileName = "MD_InvestigateSignal")]
    public sealed class MissionData : ScriptableObject
    {
        [SerializeField] private string missionName = "DEADBAND // FIRST CONTACT";
        [SerializeField] private MissionKind missionKind = MissionKind.InvestigateSignal;
        [SerializeField, TextArea] private string objectiveText =
            "Trace the signal, rescue the missing scout beneath the radio tower, and escort him to extraction.";
        [SerializeField, Range(0.1f, 1f)] private float requiredSignalStrength = 0.55f;
        [SerializeField, Min(1f)] private float extractionDuration = 8f;
        [SerializeField, Min(0f)] private float extractionNoiseRadius = 36f;

        public string MissionName => missionName;
        public MissionKind Kind => missionKind;
        public string ObjectiveText => objectiveText;
        public float RequiredSignalStrength => requiredSignalStrength;
        public float ExtractionDuration => extractionDuration;
        public float ExtractionNoiseRadius => extractionNoiseRadius;

        public void Configure()
        {
            Configure(MissionKind.InvestigateSignal, "DEADBAND // FIRST CONTACT",
                "Trace the signal, rescue the missing scout beneath the radio tower, and escort him to extraction.",
                0.55f,
                8f);
        }

        public void Configure(MissionKind kind, string title, string objective, float signalRequirement, float extractionSeconds)
        {
            missionKind = kind;
            missionName = title;
            objectiveText = objective;
            requiredSignalStrength = Mathf.Clamp(signalRequirement, 0.1f, 1f);
            extractionDuration = Mathf.Max(1f, extractionSeconds);
            extractionNoiseRadius = 36f;
        }
    }
}
