using UnityEngine;

namespace Deadband.Signals
{
    [DisallowMultipleComponent]
    public sealed class SignalHud : MonoBehaviour
    {
        [SerializeField] private SignalSystem signalSystem;
        private float surgeTimeRemaining;

        public void Configure(SignalSystem system)
        {
            signalSystem = system;
        }

        private void OnEnable()
        {
            if (signalSystem != null) signalSystem.SignalSurge += HandleSurge;
        }

        private void OnDisable()
        {
            if (signalSystem != null) signalSystem.SignalSurge -= HandleSurge;
        }

        private void Update()
        {
            surgeTimeRemaining = Mathf.Max(0f, surgeTimeRemaining - Time.unscaledDeltaTime);
        }

        private void HandleSurge(float intensity)
        {
            surgeTimeRemaining = Mathf.Max(surgeTimeRemaining, Mathf.Lerp(0.35f, 1.2f, intensity));
        }

        private void OnGUI()
        {
            if (signalSystem == null)
            {
                return;
            }

            float width = 238f;
            float height = 52f;
            float left = Screen.width - width - 18f;
            float top = Screen.height - height - 18f;
            Rect panel = new(left, top, width, height);
            Color previous = GUI.color;
            GUI.color = new Color(0.012f, 0.028f, 0.023f, 0.9f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);

            Color levelColor = GetLevelColor(signalSystem.Level);
            GUI.color = levelColor;
            GUI.DrawTexture(new Rect(left, top, 3f, height), Texture2D.whiteTexture);
            GUI.color = previous;

            GUIStyle titleStyle = new(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.65f, 1f, 0.74f) }
            };
            GUIStyle valueStyle = new(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = levelColor }
            };
            GUI.Label(new Rect(left + 12f, top + 5f, 86f, 20f), "SIGNAL", titleStyle);
            GUI.Label(
                new Rect(left + width - 62f, top + 3f, 50f, 22f),
                $"{Mathf.RoundToInt(signalSystem.Strength * 100f):00}%",
                valueStyle);

            GUI.color = new Color(0.13f, 0.18f, 0.15f, 1f);
            GUI.DrawTexture(new Rect(left + 12f, top + 29f, width - 24f, 9f), Texture2D.whiteTexture);
            GUI.color = levelColor;
            GUI.DrawTexture(new Rect(
                left + 13f,
                top + 30f,
                (width - 26f) * Mathf.Clamp01(signalSystem.Strength),
                7f), Texture2D.whiteTexture);
            GUI.color = previous;

            GUIStyle statusStyle = new(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.white }
            };
            GUI.Label(
                new Rect(left + 90f, top + 5f, width - 164f, 20f),
                BuildCompactStatus(),
                statusStyle);

            if (surgeTimeRemaining > 0f)
            {
                DrawInterference(left, top, width, height, previous);
            }
        }

        private void DrawInterference(float left, float top, float width, float height, Color previous)
        {
            float phase = Mathf.Repeat(Time.unscaledTime * 17f, 1f);
            GUI.color = new Color(1f, 0.12f, 0.38f, 0.28f);
            GUI.DrawTexture(new Rect(left + 6f, top + 5f + phase * (height - 10f), width - 12f, 2f), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private string BuildCompactStatus()
        {
            int receiverLevel = signalSystem.ReceiverUpgradeLevel;
            if (receiverLevel <= 1) return GetLevelLabel(signalSystem.Level);
            return GetCardinalDirection(signalSystem.DirectionToSource);
        }

        private string BuildReceiverGuidance()
        {
            int receiverLevel = signalSystem.ReceiverUpgradeLevel;
            if (receiverLevel <= 1)
            {
                return "MOVE AND COMPARE";
            }

            string direction = GetCardinalDirection(signalSystem.DirectionToSource);
            if (receiverLevel == 2)
            {
                return "BEARING " + direction;
            }

            float roundedDistance = Mathf.Round(signalSystem.DistanceToSource / 25f) * 25f;
            return receiverLevel == 3
                ? $"{direction}  ~{roundedDistance:0} M"
                : $"FILTERED  {direction}  ~{roundedDistance:0} M";
        }

        private static string GetCardinalDirection(Vector3 direction)
        {
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }
            string[] labels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            int index = Mathf.RoundToInt(angle / 45f) % labels.Length;
            return labels[index];
        }

        private static string GetLevelLabel(SignalLevel level)
        {
            return level switch
            {
                SignalLevel.Interference => "INTERFERENCE",
                SignalLevel.Danger => "DANGER",
                SignalLevel.HighRisk => "HIGH RISK",
                SignalLevel.SignalCore => "SIGNAL CORE",
                _ => "NORMAL"
            };
        }

        private static Color GetLevelColor(SignalLevel level)
        {
            return level switch
            {
                SignalLevel.Interference => new Color(0.75f, 1f, 0.3f),
                SignalLevel.Danger => new Color(1f, 0.72f, 0.2f),
                SignalLevel.HighRisk => new Color(1f, 0.32f, 0.12f),
                SignalLevel.SignalCore => new Color(1f, 0.12f, 0.36f),
                _ => new Color(0.35f, 1f, 0.55f)
            };
        }
    }
}
