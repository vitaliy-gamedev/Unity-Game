using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace GameFoundation.Pro.CloudSave
{
    /// <summary>
    /// Writes to Application.persistentDataPath. Useful for development and as a
    /// safe default so ICloudSaveProvider.DownloadAsync never throws just because
    /// no real backend is wired up yet — but data does NOT leave the device.
    /// Swap for a real provider before shipping cross-device save/sync.
    /// </summary>
    public class LocalCloudSaveStub : ICloudSaveProvider
    {
        private string PathFor(string key) => Path.Combine(Application.persistentDataPath, $"cloud_{key}.json");

        public Task<bool> UploadAsync(string key, string json)
        {
            try
            {
                File.WriteAllText(PathFor(key), json);
                return Task.FromResult(true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocalCloudSaveStub] Upload failed for '{key}': {e.Message}");
                return Task.FromResult(false);
            }
        }

        public Task<string> DownloadAsync(string key)
        {
            string path = PathFor(key);
            return Task.FromResult(File.Exists(path) ? File.ReadAllText(path) : null);
        }

        public Task<bool> DeleteAsync(string key)
        {
            string path = PathFor(key);
            if (File.Exists(path)) File.Delete(path);
            return Task.FromResult(true);
        }
    }
}
