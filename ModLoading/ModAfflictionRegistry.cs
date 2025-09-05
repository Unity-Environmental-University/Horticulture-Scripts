using System.Collections.Generic;
using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.ModLoading
{
    /// <summary>
    /// Registry for custom afflictions loaded from mods
    /// </summary>
    public static class ModAfflictionRegistry
    {
        private static readonly Dictionary<string, PlantAfflictions.IAffliction> _registeredAfflictions = new();
        
        /// <summary>
        /// Register a custom affliction for use in the game
        /// </summary>
        public static void Register(string name, PlantAfflictions.IAffliction affliction)
        {
            if (string.IsNullOrEmpty(name) || affliction == null) return;
            
            _registeredAfflictions[name] = affliction;
            Debug.Log($"[ModAfflictionRegistry] Registered affliction: {name}");
        }
        
        /// <summary>
        /// Get a registered affliction by name
        /// </summary>
        public static PlantAfflictions.IAffliction GetAffliction(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            _registeredAfflictions.TryGetValue(name, out var affliction);
            return affliction?.Clone(); // Always return a clone to avoid shared state
        }
        
        /// <summary>
        /// Check if an affliction is registered
        /// </summary>
        public static bool IsRegistered(string name)
        {
            return !string.IsNullOrEmpty(name) && _registeredAfflictions.ContainsKey(name);
        }
        
        /// <summary>
        /// Get all registered affliction names
        /// </summary>
        public static IEnumerable<string> GetAllAfflictionNames()
        {
            return _registeredAfflictions.Keys;
        }
        
        /// <summary>
        /// Clear all registered afflictions (for testing/cleanup)
        /// </summary>
        public static void Clear()
        {
            _registeredAfflictions.Clear();
            Debug.Log("[ModAfflictionRegistry] Cleared all registered afflictions");
        }
        
        /// <summary>
        /// Get count of registered afflictions
        /// </summary>
        public static int Count => _registeredAfflictions.Count;
    }
}