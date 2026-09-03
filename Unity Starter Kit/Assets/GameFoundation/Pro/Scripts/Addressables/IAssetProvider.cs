using System;

namespace GameFoundation.Pro.Addressables
{
    public interface IAssetProvider
    {
        void LoadAsync<T>(string key, Action<T> onLoaded) where T : UnityEngine.Object;
        void Release(string key);
    }
}
