using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace GameFoundation.Core
{
    /// <summary>
    /// Thin wrapper over Debug.Log so every log line is tagged and can be stripped
    /// from release builds in one place. [Conditional] means calls compile out
    /// entirely (zero runtime cost) in builds that don't define GF_LOGGING.
    /// Add GF_LOGGING to Scripting Define Symbols for dev/QA builds.
    /// </summary>
    public static class GFLogger
    {
        [Conditional("GF_LOGGING"), Conditional("UNITY_EDITOR")]
        public static void Log(string tag, string message)
            => Debug.Log($"[{tag}] {message}");

        [Conditional("GF_LOGGING"), Conditional("UNITY_EDITOR")]
        public static void Warn(string tag, string message)
            => Debug.LogWarning($"[{tag}] {message}");

        // Errors always log, even in release — you want to know about these from real players.
        public static void Error(string tag, string message)
            => Debug.LogError($"[{tag}] {message}");

        /// <summary>
        /// Logs a clear, specific error if a required Inspector reference wasn't
        /// assigned, naming both the owning component and the field. Returns true
        /// if the reference is present.
        ///
        /// Usage pattern (put at the very top of Awake(), before anything uses the
        /// field): call this for every required reference, combine results with
        /// &amp;= (not &amp;&amp;=, so every missing field gets reported at once
        /// instead of stopping at the first one), then bail out of Awake() if any
        /// check failed — see MainMenuWindow.cs for a full example.
        /// </summary>
        public static bool RequireField<T>(T value, string ownerName, string fieldName) where T : UnityEngine.Object
        {
            if (value != null) return true;
            Error(ownerName, $"Required field '{fieldName}' is not assigned in the Inspector.");
            return false;
        }
    }
}
