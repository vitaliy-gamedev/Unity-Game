// This file requires the "Addressables" package (com.unity.addressables) from
// Package Manager. After installing it, add the scripting define symbol
// GF_ADDRESSABLES_INSTALLED in Project Settings → Player → Scripting Define
// Symbols, otherwise this class compiles out and ResourcesAssetProvider is
// your only option — which is a perfectly fine default, nothing breaks.
#if GF_ADDRESSABLES_INSTALLED
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameFoundation.Pro.Addressables
{
    public class AddressableAssetProvider : IAssetProvider
    {
        private readonly System.Collections.Generic.Dictionary<string, AsyncOperationHandle> _handles = new();

        public void LoadAsync<T>(string key, Action<T> onLoaded) where T : UnityEngine.Object
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(key);
            _handles[key] = handle;
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                    onLoaded?.Invoke(op.Result);
                else
                    Debug.LogError($"[AddressableAssetProvider] Failed to load '{key}': {op.OperationException}");
            };
        }

        public void Release(string key)
        {
            if (_handles.TryGetValue(key, out var handle))
            {
                UnityEngine.AddressableAssets.Addressables.Release(handle);
                _handles.Remove(key);
            }
        }
    }
}
#endif
