using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Tile : MonoBehaviour
{
    public int Number;
    public TextMeshProUGUI NumberText;
    public Image background; 

    private static readonly Color[] colors = new Color[]
    {
        Color.clear,                                    // 0 - не використовується
        new Color(1f, 0.4f, 0.4f),                     // 1 - червоний
        new Color(1f, 0.6f, 0.2f),                     // 2 - помаранчевий
        new Color(1f, 0.9f, 0.2f),                     // 3 - жовтий
        new Color(0.4f, 0.9f, 0.4f),                   // 4 - зелений
        new Color(0.2f, 0.8f, 0.8f),                   // 5 - блакитний
        new Color(0.3f, 0.5f, 1f),                     // 6 - синій
        new Color(0.6f, 0.3f, 1f),                     // 7 - фіолетовий
        new Color(1f, 0.4f, 0.8f),                     // 8 - рожевий
        new Color(1f, 0.5f, 0.3f),                     // 9 - коралевий
        new Color(0.3f, 1f, 0.7f),                     // 10 - м'ятний
        new Color(1f, 0.8f, 0.4f),                     // 11 - золотий
        new Color(0.4f, 0.7f, 1f),                     // 12 - небесний
        new Color(0.9f, 0.4f, 0.6f),                   // 13 - малиновий
        new Color(0.5f, 1f, 0.5f),                     // 14 - лаймовий
        new Color(0.8f, 0.6f, 1f),                     // 15 - лавандовий
    };

    public void SetNumber(int number)
    {
        Number = number;

        if (NumberText == null)
            NumberText = GetComponentInChildren<TextMeshProUGUI>();

        if (background == null)
            background = GetComponent<Image>();

        NumberText.text = number.ToString();
        background.color = colors[number];
    }
}