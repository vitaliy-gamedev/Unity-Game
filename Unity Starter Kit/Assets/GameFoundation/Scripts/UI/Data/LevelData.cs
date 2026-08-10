using UnityEngine;

namespace GameFoundation.UI
{
    [CreateAssetMenu(fileName = "Level_", menuName = "GameFoundation/Level Data")]
    public class LevelData : ScriptableObject
    {
        public string levelId;
        public string displaySceneName;   // scene to load when the level is picked
        public Sprite thumbnail;
        public bool unlockedByDefault;
    }
}
