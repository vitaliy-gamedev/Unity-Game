using System.Threading.Tasks;

namespace GameFoundation.Pro.CloudSave
{
    /// <summary>
    /// LocalCloudSaveStub (in this package) implements this against a local file
    /// and is a genuinely useful drop-in for development/offline play — but it is
    /// NOT a real cloud backend. For actual cross-device sync, implement this same
    /// interface against Firebase/PlayFab/Google Play Saved Games and register that
    /// instead. Every call site in your game (SaveService etc.) stays unchanged.
    /// </summary>
    public interface ICloudSaveProvider
    {
        Task<bool> UploadAsync(string key, string json);
        Task<string> DownloadAsync(string key);
        Task<bool> DeleteAsync(string key);
    }
}
