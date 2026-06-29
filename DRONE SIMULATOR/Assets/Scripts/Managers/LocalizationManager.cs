using UnityEngine;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    private Dictionary<string, string[]> _strings; // key -> [UA, EN]

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildDictionary();
    }

    void BuildDictionary()
    {
        _strings = new Dictionary<string, string[]>
        {
            // Main Menu
            ["menu_play"]       = new[] { "ГРАТИ",           "PLAY" },
            ["menu_options"]    = new[] { "НАЛАШТУВАННЯ",    "OPTIONS" },
            ["menu_quit"]       = new[] { "ВИЙТИ",           "QUIT" },
            ["menu_title"]      = new[] { "СИМУЛЯТОР ДРОНА", "DRONE SIMULATOR" },

            // Drone Select
            ["select_title"]    = new[] { "ОБЕРІТЬ ДРОН",   "SELECT DRONE" },
            ["select_scout"]    = new[] { "РОЗВІДНИК\nMavic-тип\nПошук цілей", "SCOUT\nMavic-type\nTarget recon" },
            ["select_bomber"]   = new[] { "БОМБЕР\nVampir-тип\nЗнищення цілей", "BOMBER\nVampir-type\nTarget strike" },
            ["select_back"]     = new[] { "НАЗАД",           "BACK" },
            ["select_fly"]      = new[] { "ЗЛЕТІТИ",         "FLY" },

            // Options
            ["opt_title"]       = new[] { "НАЛАШТУВАННЯ",   "OPTIONS" },
            ["opt_master"]      = new[] { "ГУЧНІСТЬ",        "MASTER VOLUME" },
            ["opt_music"]       = new[] { "МУЗИКА",          "MUSIC" },
            ["opt_sfx"]         = new[] { "ЗВУКИ",           "SFX" },
            ["opt_lang"]        = new[] { "МОВА",            "LANGUAGE" },
            ["opt_back"]        = new[] { "НАЗАД",           "BACK" },
            ["opt_lang_ua"]     = new[] { "Українська",      "Ukrainian" },
            ["opt_lang_en"]     = new[] { "English",         "English" },

            // HUD - Scout
            ["hud_altitude"]    = new[] { "ВИСОТА",          "ALTITUDE" },
            ["hud_speed"]       = new[] { "ШВИДКІСТЬ",       "SPEED" },
            ["hud_battery"]     = new[] { "БАТАРЕЯ",         "BATTERY" },
            ["hud_targets"]     = new[] { "ЦІЛІ",            "TARGETS" },
            ["hud_mark"]        = new[] { "[E] Позначити ціль", "[E] Mark Target" },
            ["hud_return"]      = new[] { "[ESC] Повернутись", "[ESC] Return" },

            // HUD - Bomber
            ["hud_payload"]     = new[] { "БОЄКОМПЛЕКТ",     "PAYLOAD" },
            ["hud_drop"]        = new[] { "[ПРОБІЛ] Скинути", "[SPACE] Drop" },
            ["hud_lock"]        = new[] { "ЗАХОПЛЕННЯ",      "LOCK ON" },
            ["hud_no_targets"]  = new[] { "Немає позначених цілей.\nПотрібен вильот розвідника!", "No marked targets.\nScout mission required!" },

            // Mission
            ["mission_scout_complete"] = new[] { "РОЗВІДКА ЗАВЕРШЕНА\nЦілей позначено:", "RECON COMPLETE\nTargets marked:" },
            ["mission_bomber_complete"] = new[] { "МІСІЯ ВИКОНАНА\nЦілей знищено:", "MISSION COMPLETE\nTargets destroyed:" },
            ["btn_return_menu"]  = new[] { "В МЕНЮ",         "MAIN MENU" },

            // Targets
            ["target_infantry"] = new[] { "Піхота",          "Infantry" },
            ["target_vehicle"]  = new[] { "Техніка",         "Vehicle" },
            ["target_bunker"]   = new[] { "Укріплення",      "Bunker" },
        };
    }

    public string Get(string key)
    {
        var lang = GameManager.Instance != null
            ? GameManager.Instance.currentLanguage
            : GameManager.Language.Ukrainian;

        if (_strings.TryGetValue(key, out var pair))
            return pair[(int)lang];

        Debug.LogWarning($"[Localization] Missing key: {key}");
        return key;
    }

    public void ApplyLanguage(GameManager.Language lang)
    {
        // Notify all LocalizedText components in scene
        var all = FindObjectsOfType<LocalizedText>();
        foreach (var lt in all) lt.Refresh();
    }
}
