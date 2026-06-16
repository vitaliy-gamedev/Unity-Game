using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BuildSlot : MonoBehaviour
{
    [Tooltip("Яку категорію айтемів приймає слот. Порожньо = приймає будь-який")]
    [SerializeField] private string acceptedCategory = "";

    [Header("Підсвічування (необов'язково)")]
    [Tooltip("SpriteRenderer, який змінює колір. Можна перетягнути сам об'єкт слота")]
    [SerializeField] private SpriteRenderer highlightRenderer;
    [SerializeField] private Color idleColor    = new(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color validColor   = new(0.4f, 1f, 0.4f, 0.6f);
    [SerializeField] private Color invalidColor = new(1f, 0.4f, 0.4f, 0.6f);

    public bool IsOccupied { get; private set; }
    public GameObject CurrentBuilding { get; private set; }
    public BuildingData CurrentData { get; private set; }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        ResetHighlight();
    }
    
    public bool CanAccept(BuildingData data)
    {
        if (IsOccupied || data == null) return false;
        if (string.IsNullOrEmpty(acceptedCategory)) return true;
        return acceptedCategory == data.category;
    }

    public void ShowHighlight(bool valid)
    {
        if (highlightRenderer != null)
            highlightRenderer.color = valid ? validColor : invalidColor;
    }

    public void ResetHighlight()
    {
        if (highlightRenderer != null)
            highlightRenderer.color = idleColor;
    }
    
    public GameObject Place(BuildingData data)
    {
        if (!CanAccept(data) || data.worldPrefab == null) return null;

        CurrentBuilding = Instantiate(data.worldPrefab, transform.position, Quaternion.identity, transform);
        CurrentData = data;
        IsOccupied = true;
        ResetHighlight();
        
        var building = CurrentBuilding.GetComponent<Building>();
        if (building == null) building = CurrentBuilding.AddComponent<Building>();
        building.Init(this);

        return CurrentBuilding;
    }
    
    public void Clear()
    {
        if (CurrentBuilding != null) Destroy(CurrentBuilding);
        CurrentBuilding = null;
        CurrentData = null;
        IsOccupied = false;
        ResetHighlight();
    }
    
    public void Demolish()
    {
        if (!IsOccupied) return;

        if (CurrentData != null && CurrentData.costAmount > 0 && ResourceManager.Instance != null)
        {
            int refund = Mathf.RoundToInt(CurrentData.costAmount * CurrentData.refundFraction);
            if (refund > 0) ResourceManager.Instance.Add(CurrentData.costType, refund);
        }

        Clear();
    }
}
