using System;
using System.IO;
using UnityEngine;

namespace GameFoundation.Core
{
    /// <summary>
    /// Generic JSON save/load. Works with any [Serializable] class or struct via
    /// JsonUtility. Each key becomes its own file at
    /// {persistentDataPath}/Saves/{key}.json — simple to inspect/delete by hand
    /// during development, and safe from one save corrupting another.
    /// </summary>
    public class SaveService : ISaveService
    {
        private readonly string _saveFolder;

        public SaveService()
        {
            _saveFolder = Path.Combine(Application.persistentDataPath, "Saves");
            Directory.CreateDirectory(_saveFolder);
        }

        public void Save<T>(string key, T data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(PathFor(key), json);
                GFLogger.Log("SaveService", $"Saved '{key}'.");
            }
            catch (Exception e)
            {
                GFLogger.Error("SaveService", $"Failed to save '{key}': {e.Message}");
            }
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            string path = PathFor(key);
            if (!File.Exists(path))
                return defaultValue;

            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                GFLogger.Error("SaveService", $"Failed to load '{key}', returning default: {e.Message}");
                return defaultValue;
            }
        }

        public bool HasSave(string key) => File.Exists(PathFor(key));

        public void DeleteSave(string key)
        {
            string path = PathFor(key);
            if (File.Exists(path))
                File.Delete(path);
        }

        private string PathFor(string key) => Path.Combine(_saveFolder, $"{key}.json");
    }
}
