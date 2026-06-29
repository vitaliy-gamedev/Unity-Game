using UnityEngine;
using TMPro; 

public class ResourceManager : MonoBehaviour
{
    
    [SerializeField] private TextMeshProUGUI _resourcesText;
    

    private void Update()
    {
        UpdateResourceUI();
    }

    private void UpdateResourceUI()
    {
        if (_resourcesText == null) return;

        int totalResources = 0;

        
        foreach (var building in Unit.AllBuildings)
        {
            if (building != null)
            {
                totalResources += building.StoredResources;
            }
        }

        
        _resourcesText.text = $"Resources: {totalResources}";
    }
}