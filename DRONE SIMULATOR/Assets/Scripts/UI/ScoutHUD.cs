using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// HUD for the Scout mission scene. Attach to the HUD Canvas.
/// Wire: droneController, all TMP_Text fields, targetListPanel.
/// </summary>
public class ScoutHUD : MonoBehaviour
{
    [Header("Drone Reference")]
    public ScoutDroneController droneController;

    [Header("Status Bar")]
    public TMP_Text altitudeText;
    public TMP_Text speedText;
    public TMP_Text batteryText;
    public TMP_Text targetCountText;
    public Slider   batterySlider;

    [Header("Detection")]
    public GameObject detectReticle;    // crosshair shown when target in sensor
    public TMP_Text   detectLabel;      // "Ціль виявлена / Target detected"
    public TMP_Text   markPromptText;   // "[E] Позначити"

    [Header("Target List Panel")]
    public Transform targetListParent;  // scroll view content
    public GameObject targetEntryPrefab; // TMP_Text prefab for each entry

    [Header("Marked Marker Prefab")]
    public GameObject worldMarkerPrefab; // 3D world-space marker above marked targets

    private int             _targetCount = 0;
    private List<TMP_Text>  _entryTexts  = new List<TMP_Text>();

    void Start()
    {
        if (droneController == null) droneController = FindObjectOfType<ScoutDroneController>();

        droneController.OnHUDUpdate     += UpdateStatus;
        droneController.OnTargetDetected += UpdateDetection;
        droneController.OnTargetMarked  += AddTargetEntry;

        if (markPromptText != null)
        {
            var loc = LocalizationManager.Instance;
            markPromptText.text = loc != null ? loc.Get("hud_mark") : "[E] Mark";
        }

        SetDetection(null);
    }

    void UpdateStatus(Vector3 pos, float speed, float battery)
    {
        var loc  = LocalizationManager.Instance;
        string altLabel = loc?.Get("hud_altitude") ?? "ALT";
        string spdLabel = loc?.Get("hud_speed")    ?? "SPD";
        string batLabel = loc?.Get("hud_battery")  ?? "BAT";

        if (altitudeText != null) altitudeText.text = $"{altLabel}: {pos.y:F1}m";
        if (speedText    != null) speedText.text    = $"{spdLabel}: {speed * 3.6f:F1}km/h";
        if (batteryText  != null) batteryText.text  = $"{batLabel}: {battery:F0}%";
        if (batterySlider != null) batterySlider.value = battery / 100f;

        // Battery color warning
        if (batteryText != null)
            batteryText.color = battery < 20f ? Color.red : battery < 40f ? Color.yellow : Color.white;
    }

    void UpdateDetection(GameObject target)
    {
        SetDetection(target);
    }

    void SetDetection(GameObject target)
    {
        bool found = target != null;
        if (detectReticle != null) detectReticle.SetActive(found);
        if (markPromptText != null) markPromptText.gameObject.SetActive(found);
        if (detectLabel != null)
        {
            detectLabel.gameObject.SetActive(found);
            if (found)
            {
                var lang = GameManager.Instance?.currentLanguage ?? GameManager.Language.Ukrainian;
                var type = target.GetComponent<TargetEntity>()?.targetType ?? "";
                detectLabel.text = lang == GameManager.Language.Ukrainian
                    ? $"ВИЯВЛЕНО: {type}"
                    : $"DETECTED: {type}";
            }
        }
    }

    void AddTargetEntry(TargetData data)
    {
        _targetCount++;
        var loc  = LocalizationManager.Instance;
        var lang = GameManager.Instance?.currentLanguage ?? GameManager.Language.Ukrainian;

        if (targetCountText != null)
        {
            string label = loc?.Get("hud_targets") ?? "Targets";
            targetCountText.text = $"{label}: {_targetCount}";
        }

        // Spawn world marker
        if (worldMarkerPrefab != null)
        {
            var marker = Instantiate(worldMarkerPrefab,
                data.worldPosition + Vector3.up * 3f, Quaternion.identity);
            Destroy(marker, 300f); // cleanup after 5 min
        }

        // Add to scrollable list
        if (targetListParent != null)
        {
            TMP_Text entry;
            if (targetEntryPrefab != null)
            {
                var go = Instantiate(targetEntryPrefab, targetListParent);
                entry  = go.GetComponentInChildren<TMP_Text>();
            }
            else
            {
                var go  = new GameObject($"Entry_{_targetCount}");
                go.transform.SetParent(targetListParent, false);
                entry   = go.AddComponent<TMP_Text>();
                entry.fontSize = 14f;
                entry.color    = Color.white;
            }

            if (entry != null)
            {
                entry.text = data.ToDisplayString(lang);
                _entryTexts.Add(entry);
            }
        }

        // Flash screen briefly
        StartCoroutine(FlashConfirmation());
    }

    System.Collections.IEnumerator FlashConfirmation()
    {
        // Quick camera flash effect — requires a full-screen overlay image named "FlashOverlay"
        var overlay = transform.Find("FlashOverlay")?.GetComponent<Image>();
        if (overlay == null) yield break;
        overlay.color = new Color(0f, 1f, 0.5f, 0.4f);
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            overlay.color = new Color(0f, 1f, 0.5f, Mathf.Lerp(0.4f, 0f, t / 0.4f));
            yield return null;
        }
    }
}
