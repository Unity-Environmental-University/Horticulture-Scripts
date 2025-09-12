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
            
            // Replace existing bundle if hot-reloading
            if (Bundles.TryGetValue(key, out var existing) && existing)
            {
                try { existing.Unload(false); } catch { /* ignore */ }
            }

            Bundles[key] = bundle;
        }

        public static void UnregisterAndUnload(string key, bool unloadAllLoadedObjects = false)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (!Bundles.TryGetValue(key, out var bundle) || bundle == null) { Bundles.Remove(key); return; }
            try { bundle.Unload(unloadAllLoadedObjects); } catch { /* ignore */ }
            Bundles.Remove(key);
        }

        public static void UnloadAll(bool unloadAllLoadedObjects = false)
        {
            foreach (var kv in Bundles)
            {
                var b = kv.Value;
                if (!b) continue;
                try { b.Unload(unloadAllLoadedObjects); } catch { /* ignore */ }
            }
            Bundles.Clear();
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
