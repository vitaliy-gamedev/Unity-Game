using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Build System/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Ідентифікація")]
    public string id;
    public string displayName;

    [Tooltip("Іконка для кнопки в UI-панелі")]
    public Sprite icon;

    [Tooltip("Спрайт привида/прев'ю у світі під час перетягування. Якщо порожньо — береться icon")]
    public Sprite worldSprite;

    [Header("Що спавнити на сцені")]
    [Tooltip("Префаб, який з'явиться у слоті (будівля, юніт тощо)")]
    public GameObject worldPrefab;

    [Header("Сумісність зі слотом")]
    [Tooltip("Слот прийме айтем, лише якщо його acceptedCategory збігається з цим значенням. Порожньо = підійде будь-який слот")]
    public string category = "";

    [Header("Вартість будівництва")]
    public ResourceType costType = ResourceType.Gold;

    [Tooltip("0 = безкоштовно")]
    public int costAmount = 0;

    [Header("Знесення")]
    [Tooltip("Частка вартості, яка повертається при знесенні (0 = нічого, 0.5 = половина, 1 = все)")]
    [Range(0f, 1f)]
    public float refundFraction = 0.5f;
}
