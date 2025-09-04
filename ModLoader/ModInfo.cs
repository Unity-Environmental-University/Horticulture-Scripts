using System;
using UnityEngine;

namespace _project.Scripts.ModLoader
{
    /// <summary>
    /// Simple mod information structure for basic mod metadata
    /// </summary>
    [Serializable]
    public class ModInfo
    {
        public string name = "Unknown Mod";
        public string version = "1.0.0";
        public string author = "";
        public string description = "";
        
        /// <summary>
        /// Load mod info from JSON text
        /// </summary>
        public static ModInfo FromJson(string json)
        {
            try 
            { 
                return JsonUtility.FromJson<ModInfo>(json) ?? new ModInfo(); 
            }
            catch 
            { 
                return new ModInfo(); 
            }
        }
    }
}