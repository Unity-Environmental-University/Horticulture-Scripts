using System.Collections.Generic;
using UnityEngine;

namespace _project.Scripts.ModLoading
{
    /// <summary>
    /// Registry for mod-loaded AssetBundles and helper asset resolution.
    /// </summary>
    public static class ModAssets
    {
        private static readonly Dictionary<string, AssetBundle> Bundles = new();

        public static void RegisterBundle(string key, AssetBundle bundle)
        {
            if (string.IsNullOrWhiteSpace(key) || bundle == null) return;
            if (!Bundles.TryAdd(key, bundle)) return;
        }

        public static T LoadFromBundle<T>(string key, string assetName) where T : Object
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(assetName)) return null;
            if (!Bundles.TryGetValue(key, out var bundle) || bundle == null) return null;
            try
            {
                return bundle.LoadAsset<T>(assetName);
            }
            catch
            {
                return null;
            }
        }
    }
}
