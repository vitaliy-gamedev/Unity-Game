using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static readonly string FileName = "save.json";

    private static string FilePath
    {
        get
        {
            string dir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, FileName);
        }
    }

    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"[SaveManager] Progress saved: {FilePath}");
        }
        catch (IOException ex)
        {
            Debug.LogError($"[SaveManager] Write error: {ex.Message}");
        }
    }

    public static SaveData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                if (data != null)
                {
                    Debug.Log($"[SaveManager] Progress loaded: {FilePath}");
                    return data;
                }

                Debug.LogWarning("[SaveManager] Corrupted save, creating new.");
            }
            else
            {
                Debug.Log("[SaveManager] Save file not found, creating new.");
            }
        }
        catch (IOException ex)
        {
            Debug.LogError($"[SaveManager] Read error: {ex.Message}");
        }
        catch (System.ArgumentException ex)
        {
            Debug.LogError($"[SaveManager] Invalid JSON: {ex.Message}");
        }

        return new SaveData();
    }

    public static void DeleteSave()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
            Debug.Log("[SaveManager] Save deleted.");
        }
    }
}
