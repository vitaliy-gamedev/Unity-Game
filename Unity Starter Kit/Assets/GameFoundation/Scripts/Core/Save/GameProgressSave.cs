using System;

namespace GameFoundation.Core
{
    [Serializable]
    public class GameProgressSave
    {
        public const string AutosaveKey = "deadband_autosave";

        public string lastSceneName;
        public string savedAtUtc;

        public static GameProgressSave NewGame() => new()
        {
            lastSceneName = string.Empty,
            savedAtUtc = DateTime.UtcNow.ToString("O")
        };
    }
}
