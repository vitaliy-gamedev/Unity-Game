using UnityEngine;

namespace GameFoundation.Pro.Achievements
{
    [CreateAssetMenu(fileName = "Achievement_", menuName = "GameFoundation/Achievement Definition")]
    public class AchievementDefinition : ScriptableObject
    {
        public string id;
        public string titleKey;
        public string descriptionKey;
        public Sprite icon;
        public int targetProgress = 1; // 1 = simple unlock, >1 = incremental (e.g. "kill 100 enemies")
    }
}
