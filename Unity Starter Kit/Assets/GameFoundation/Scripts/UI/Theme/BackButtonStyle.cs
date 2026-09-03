using GameFoundation.Pro.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>Keeps every menu's back button at the same size, position and Pro theme.</summary>
    public static class BackButtonStyle
    {
        private static readonly Vector2 Size = new(220f, 64f);
        private static readonly Vector2 Position = new(42f, -38f);

        public static void Apply(Button button)
        {
            if (button == null) return;

            if (button.transform is RectTransform rect)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = Position;
                rect.sizeDelta = Size;
            }

            if (button.targetGraphic is Image image)
            {
                image.material = null;
                AddTheme(image, ThemeColorRole.Secondary);

                var outline = image.GetComponent<Outline>();
                if (outline == null)
                    outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.42f, 0.58f, 0.20f, 0.72f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
                outline.useGraphicAlpha = true;
            }

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 25f;
                label.fontWeight = FontWeight.SemiBold;
                label.enableAutoSizing = false;
                label.alignment = TextAlignmentOptions.Center;
                AddTheme(label, ThemeColorRole.TextPrimary);
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 1f, 0.72f, 1f);
            colors.pressedColor = new Color(0.58f, 0.72f, 0.38f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        private static void AddTheme(Graphic graphic, ThemeColorRole role)
        {
            var applier = graphic.GetComponent<ThemeApplier>();
            if (applier == null)
                applier = graphic.gameObject.AddComponent<ThemeApplier>();
            applier.Configure(role);
        }
    }
}
