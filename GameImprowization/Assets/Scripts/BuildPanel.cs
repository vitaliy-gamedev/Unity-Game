using System.Collections.Generic;
using UnityEngine;

public class BuildPanel : MonoBehaviour
{
    [Tooltip("Список айтемів, які показуються в панелі")]
    [SerializeField] private List<BuildingData> buildings = new();

    [Tooltip("Префаб кнопки-айтема (Image + DraggableBuildItem)")]
    [SerializeField] private DraggableBuildItem itemButtonPrefab;

    [Tooltip("Куди складати кнопки. Порожньо = у цей самий об'єкт")]
    [SerializeField] private Transform container;

    private void Start()
    {
        if (container == null) container = transform;
        if (itemButtonPrefab == null) return;

        foreach (var data in buildings)
        {
            if (data == null) continue;
            var item = Instantiate(itemButtonPrefab, container);
            item.SetData(data);
            item.name = $"BuildItem_{data.displayName}";
        }
    }
}
