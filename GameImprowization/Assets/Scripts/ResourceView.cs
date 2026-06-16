using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ResourceView : MonoBehaviour
{
    [Tooltip("Текстове поле, у яке виводяться ресурси")]
    [SerializeField] private Text label;

    private void Start()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (label == null || ResourceManager.Instance == null) return;

        var sb = new StringBuilder();
        foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
            sb.AppendLine($"{t}: {ResourceManager.Instance.Get(t)}");

        label.text = sb.ToString();
    }
}
