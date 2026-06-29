using UnityEngine;

/// <summary>
/// Attach to any target object in Scout/Bomber scenes.
/// Assign targetType and the script will self-setup layer + collider.
/// </summary>
public class TargetEntity : MonoBehaviour
{
    public enum TargetTypeEnum { Infantry, Vehicle, Bunker }

    [Header("Identity")]
    public TargetTypeEnum type = TargetTypeEnum.Infantry;
    public string targetType => type.ToString();

    [Header("State")]
    public bool isDestroyed = false;
    public bool isMarked    = false;

    [Header("Visuals")]
    public Color normalColor   = Color.green;
    public Color markedColor   = Color.red;
    public Color destroyedColor = Color.gray;

    private Renderer[] _renderers;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        SetColor(normalColor);
    }

    public void MarkTarget()
    {
        isMarked = true;
        SetColor(markedColor);
    }

    public void DestroyTarget()
    {
        isDestroyed = true;
        SetColor(destroyedColor);
        // Disable movement if any
        var movement = GetComponent<TargetMovement>();
        if (movement != null) movement.enabled = false;
    }

    void SetColor(Color c)
    {
        foreach (var r in _renderers)
            if (r != null) r.material.color = c;
    }
}
