using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Pro.Achievements
{
    /// <summary>
    /// Fully working, no-SDK-required achievement backend. Progress and unlock
    /// state persist in PlayerPrefs under a per-achievement key prefix.
    /// Register: ServiceLocator.Register&lt;IAchievementService&gt;(new LocalAchievementService(definitions));
    /// </summary>
    public class LocalAchievementService : IAchievementService
    {
        private const string ProgressKeyPrefix = "gf_pro_ach_progress_";
        private const string UnlockedKeyPrefix = "gf_pro_ach_unlocked_";

        private readonly Dictionary<string, AchievementDefinition> _definitions = new();

        public event Action<string> OnUnlocked;

        public LocalAchievementService(IEnumerable<AchievementDefinition> definitions)
        {
            if (definitions == null) return;

            foreach (var def in definitions)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                {
                    Debug.LogWarning("[LocalAchievementService] Ignored an empty achievement definition or id.");
                    continue;
                }

                _definitions[def.id] = def;
            }
        }

        public void Unlock(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                Debug.LogWarning("[LocalAchievementService] Cannot unlock an achievement with an empty id.");
                return;
            }

            if (IsUnlocked(achievementId)) return;

            PlayerPrefs.SetInt(UnlockedKeyPrefix + achievementId, 1);
            PlayerPrefs.Save();
            OnUnlocked?.Invoke(achievementId);
        }

        public void IncrementProgress(string achievementId, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(achievementId) || amount <= 0)
            {
                Debug.LogWarning("[LocalAchievementService] Achievement id must be set and progress amount must be positive.");
                return;
            }

            if (IsUnlocked(achievementId)) return;

            if (!_definitions.TryGetValue(achievementId, out var def))
            {
                Debug.LogWarning($"[LocalAchievementService] Unknown achievement id '{achievementId}'.");
                return;
            }

            int newProgress = GetProgress(achievementId) + amount;
            PlayerPrefs.SetInt(ProgressKeyPrefix + achievementId, newProgress);
            PlayerPrefs.Save();

            if (newProgress >= def.targetProgress)
                Unlock(achievementId);
        }

        public bool IsUnlocked(string achievementId)
            => !string.IsNullOrWhiteSpace(achievementId)
               && PlayerPrefs.GetInt(UnlockedKeyPrefix + achievementId, 0) == 1;

        public int GetProgress(string achievementId)
            => string.IsNullOrWhiteSpace(achievementId)
                ? 0
                : PlayerPrefs.GetInt(ProgressKeyPrefix + achievementId, 0);
    }
}
