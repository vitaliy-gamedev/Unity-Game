using System;
using UnityEngine;

namespace GameFoundation.Pro.Addressables
{
    /// <summary>
    /// No-dependency implementation using Resources.LoadAsync — works with zero
    /// extra packages installed. Use this while prototyping, or for projects that
    /// never grow large enough to need Addressables' memory management.
    /// </summary>
    public class ResourcesAssetProvider : IAssetProvider
    {
        public void LoadAsync<T>(string key, Action<T> onLoaded) where T : UnityEngine.Object
        {
            var request = Resources.LoadAsync<T>(key);
            request.completed += _ => onLoaded?.Invoke(request.asset as T);
        }

        public void Release(string key)
        {
            // Resources-loaded assets are reference counted by Unity itself;
            // nothing to explicitly release here. Present for interface parity
            // with AddressableAssetProvider.
        }
    }
}
