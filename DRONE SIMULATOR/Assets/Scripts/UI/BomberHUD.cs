using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD for the Bomber mission scene. Attach to HUD Canvas.
/// </summary>
public class BomberHUD : MonoBehaviour
{
    [Header("Drone Reference")]
    public BomberDroneController droneController;

    [Header("Status")]
    public TMP_Text altitudeText;
    public TMP_Text speedText;
    public TMP_Text batteryText;
    public Slider   batterySlider;

    [Header("Payload")]
    public TMP_Text payloadText;
    public Image[]  payloadIcons;       // array of bomb icons (grey out when spent)
    public Color    activePayloadColor  = Color.white;
    public Color    spentPayloadColor   = new Color(0.3f, 0.3f, 0.3f);

    [Header("Lock On")]
    public GameObject lockOnReticle;    // animated crosshair
    public TMP_Text   lockOnText;       // distance + grid
    public TMP_Text   dropPromptText;   // "[SPACE] Drop"

    [Header("Destroyed Log")]
    public Transform destroyedListParent;
    public GameObject destroyedEntryPrefab;

    [Header("No Targets Warning")]
    public GameObject noTargetsPanel;

    private int _lastPayload = -1;

    void Start()
    {
        if (droneController == null) droneController = FindObjectOfType<BomberDroneController>();

        droneController.OnHUDUpdate      += UpdateStatus;
        droneController.OnPayloadUpdate  += UpdatePayload;
        droneController.OnLockUpdate     += UpdateLockOn;
        droneController.OnTargetDestroyed += AddDestroyedEntry;

        var loc  = LocalizationManager.Instance;
        if (dropPromptText != null)
            dropPromptText.text = loc?.Get("hud_drop") ?? "[SPACE] Drop";

        // Show no-targets warning if needed
        bool hasTargets = GameManager.Instance != null &&
                          GameManager.Instance.GetMarkedTargets().Length > 0;
        if (noTargetsPanel != null)
            noTargetsPanel.SetActive(!hasTargets);

        SetLockOn(null, -1f);
    }

    void UpdateStatus(Vector3 pos, float speed, float battery)
    {
        var loc = LocalizationManager.Instance;
        if (altitudeText != null) altitudeText.text = $"{loc?.Get("hud_altitude") ?? "ALT"}: {pos.y:F1}m";
        if (speedText    != null) speedText.text    = $"{loc?.Get("hud_speed")    ?? "SPD"}: {speed * 3.6f:F1}km/h";
        if (batteryText  != null)
        {
            batteryText.text  = $"{loc?.Get("hud_battery") ?? "BAT"}: {battery:F0}%";
            batteryText.color = battery < 20f ? Color.red : battery < 40f ? Color.yellow : Color.white;
        }
        if (batterySlider != null) batterySlider.value = battery / 100f;
    }

    void UpdatePayload(int current, int max)
    {
        var loc  = LocalizationManager.Instance;
        if (payloadText != null)
            payloadText.text = $"{loc?.Get("hud_payload") ?? "PAYLOAD"}: {current}/{max}";

        // Update icons
        for (int i = 0; i < payloadIcons.Length; i++)
        {
            if (payloadIcons[i] != null)
                payloadIcons[i].color = i < current ? activePayloadColor : spentPayloadColor;
        }

        _lastPayload = current;
    }

    void UpdateLockOn(TargetData target, float distance)
    {
        SetLockOn(target, distance);
    }

    void SetLockOn(TargetData target, float distance)
    {
        bool locked = target != null && distance >= 0f;
        if (lockOnReticle != null) lockOnReticle.SetActive(locked);
        if (dropPromptText != null) dropPromptText.gameObject.SetActive(locked);

        if (lockOnText != null)
        {
            lockOnText.gameObject.SetActive(locked);
            if (locked)
            {
                var lang = GameManager.Instance?.currentLanguage ?? GameManager.Language.Ukrainian;
                string lockLabel = lang == GameManager.Language.Ukrainian ? "ЗАХОПЛЕННЯ" : "LOCK ON";
                lockOnText.text = $"{lockLabel}\n{target.gridCoordinate}  {distance:F0}m";
            }
        }
    }

    void AddDestroyedEntry(TargetData data)
    {
        if (destroyedListParent == null) return;
        var lang = GameManager.Instance?.currentLanguage ?? GameManager.Language.Ukrainian;

        TMP_Text entry;
        if (destroyedEntryPrefab != null)
        {
            entry = Instantiate(destroyedEntryPrefab, destroyedListParent)
                        .GetComponentInChildren<TMP_Text>();
        }
        else
        {
            var go = new GameObject("Destroyed_" + data.gridCoordinate);
            go.transform.SetParent(destroyedListParent, false);
            entry = go.AddComponent<TMP_Text>();
            entry.fontSize = 14f;
            entry.color    = new Color(1f, 0.4f, 0.2f);
        }

        if (entry != null)
        {
            string prefix = lang == GameManager.Language.Ukrainian ? "✓ ЗНИЩЕНО" : "✓ DESTROYED";
            entry.text = $"{prefix}: {data.targetType} [{data.gridCoordinate}]";
        }
    }
}
