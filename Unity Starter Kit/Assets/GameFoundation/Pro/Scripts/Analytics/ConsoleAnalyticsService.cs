using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameFoundation.Pro.Analytics
{
    /// <summary>
    /// Logs every event to the console with a timestamp — genuinely useful during
    /// development for verifying your event calls fire at the right moments.
    /// When you're ready for a real backend, write an IAnalyticsService that wraps
    /// the Firebase/GameAnalytics/Unity Analytics SDK and register that instead;
    /// every LogEvent/LogScreenView call site in your game stays untouched.
    /// </summary>
    public class ConsoleAnalyticsService : IAnalyticsService
    {
        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            Debug.Log($"[Analytics] {eventName}{FormatParams(parameters)}");
        }

        public void LogScreenView(string screenName)
        {
            Debug.Log($"[Analytics] screen_view: {screenName}");
        }

        private static string FormatParams(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0) return string.Empty;

            var sb = new StringBuilder(" { ");
            bool first = true;
            foreach (var kvp in parameters)
            {
                if (!first) sb.Append(", ");
                sb.Append(kvp.Key).Append('=').Append(kvp.Value);
                first = false;
            }
            sb.Append(" }");
            return sb.ToString();
        }
    }
}
