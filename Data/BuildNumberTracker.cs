using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace _project.Scripts.Data
{
    [Serializable]
    public class BuildInfo
    {
        public int buildNumber;
    }
    
    public static class BuildNumberTracker
    {
        private const string FilePath = "Assets/Resources/BuildInfo.json";

        public static int IncrementBuildNumber()
        {
            var json = File.ReadAllText(FilePath);
            var info = JsonUtility.FromJson<BuildInfo>(json);
            info.buildNumber++;
            File.WriteAllText(FilePath, JsonUtility.ToJson(info, true));
            return info.buildNumber;
        }

        public static string GetFullBundleVersion(int buildNumber)
        {
            var currentVersion = PlayerSettings.bundleVersion;
            var parts = currentVersion.Split('.');
            var coreVersion = string.Join(".", parts.Length > 3 ? parts[..3] : parts);
            return $"{coreVersion}.{buildNumber}";
        }
    }
}