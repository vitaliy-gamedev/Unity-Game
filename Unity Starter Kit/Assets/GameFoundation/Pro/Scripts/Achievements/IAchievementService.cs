using System;

namespace GameFoundation.Pro.Achievements
{
    /// <summary>
    /// LocalAchievementService (in this package) implements this against PlayerPrefs
    /// and works fully offline out of the box. To hook up Steamworks.NET or Google
    /// Play Games Services later, write a second implementation of this same
    /// interface that forwards Unlock/IncrementProgress calls to that SDK, and
    /// register it in ServiceLocator instead — nothing else in your game changes.
    /// </summary>
    public interface IAchievementService
    {
        void Unlock(string achievementId);
        void IncrementProgress(string achievementId, int amount = 1);
        bool IsUnlocked(string achievementId);
        int GetProgress(string achievementId);

        event Action<string> OnUnlocked;
    }
}
