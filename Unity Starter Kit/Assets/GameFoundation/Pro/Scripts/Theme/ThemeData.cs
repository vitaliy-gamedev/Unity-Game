using TMPro;
using UnityEngine;

namespace GameFoundation.Pro.Theme
{
    [CreateAssetMenu(fileName = "Theme_", menuName = "GameFoundation/Theme Data")]
    public class ThemeData : ScriptableObject
    {
        [Header("Colors")]
        public Color primary = Color.white;
        public Color secondary = Color.gray;
        public Color accent = Color.yellow;
        public Color background = Color.black;
        public Color textPrimary = Color.white;
        public Color textSecondary = Color.gray;

        [Header("Typography")]
        public TMP_FontAsset headingFont;
        public TMP_FontAsset bodyFont;

        [Header("Graphics")]
        public Sprite buttonSprite;
        public Sprite panelSprite;
    }
}
