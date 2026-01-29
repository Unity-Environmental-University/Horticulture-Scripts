using System;
using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Handlers;

namespace _project.Scripts.PlayModeTest.Utilities.Reflection
{
    /// <summary>
    ///     Provides cached reflection access to TreatmentEfficacyHandler's private fields and methods.
    ///     Use this instead of inline reflection to improve test performance and reduce code duplication.
    /// </summary>
    public static class TreatmentEfficacyHandlerReflection
    {
        private static readonly FieldInfo DiscoveredCombinationsField;
        private static readonly MethodInfo MakeDiscoveryKeyMethod;
        private static readonly MethodInfo SaveDiscoveryStateMethod;
        private static readonly MethodInfo LoadDiscoveryDataMethod;
        private static readonly MethodInfo DiscoveryDataExistsMethod;
        private static readonly MethodInfo MarkAsDiscoveredMethod;

        static TreatmentEfficacyHandlerReflection()
        {
            const BindingFlags instanceFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            const BindingFlags staticFlags = BindingFlags.NonPublic | BindingFlags.Static;

            DiscoveredCombinationsField =
                typeof(TreatmentEfficacyHandler).GetField("discoveredCombinations", instanceFlags);
            MakeDiscoveryKeyMethod = typeof(TreatmentEfficacyHandler).GetMethod("MakeDiscoveryKey", staticFlags);
            SaveDiscoveryStateMethod = typeof(TreatmentEfficacyHandler).GetMethod("SaveDiscoveryState", staticFlags);
            LoadDiscoveryDataMethod = typeof(TreatmentEfficacyHandler).GetMethod("LoadDiscoveryData", staticFlags);
            DiscoveryDataExistsMethod = typeof(TreatmentEfficacyHandler).GetMethod("DiscoveryDataExists", staticFlags);
            MarkAsDiscoveredMethod = typeof(TreatmentEfficacyHandler).GetMethod("MarkAsDiscovered", instanceFlags);

            // Validate all reflection targets were found
            if (DiscoveredCombinationsField == null)
                throw new InvalidOperationException(
                    "TreatmentEfficacyHandler.discoveredCombinations field not found via reflection. Class may have been refactored.");
            if (MakeDiscoveryKeyMethod == null)
                throw new InvalidOperationException(
                    "TreatmentEfficacyHandler.MakeDiscoveryKey method not found via reflection. Class may have been refactored.");
            if (SaveDiscoveryStateMethod == null)
                throw new InvalidOperationException(
                    "TreatmentEfficacyHandler.SaveDiscoveryState method not found via reflection. Class may have been refactored.");
            if (LoadDiscoveryDataMethod == null)
                throw new InvalidOperationException(
                    "TreatmentEfficacyHandler.LoadDiscoveryData method not found via reflection. Class may have been refactored.");
            if (DiscoveryDataExistsMethod == null)
                throw new InvalidOperationException(
                    "TreatmentEfficacyHandler.DiscoveryDataExists method not found via reflection. Class may have been refactored.");
            if (MarkAsDiscoveredMethod == null)
                throw new InvalidOperationException(
                    "TreatmentEfficacyHandler.MarkAsDiscovered method not found via reflection. Class may have been refactored.");
        }

        /// <summary>
        ///     Gets the private discoveredCombinations HashSet from a TreatmentEfficacyHandler instance.
        /// </summary>
        public static HashSet<string> GetDiscoveredCombinations(TreatmentEfficacyHandler handler)
        {
            return DiscoveredCombinationsField?.GetValue(handler) as HashSet<string>;
        }

        /// <summary>
        ///     Sets the private discoveredCombinations HashSet on a TreatmentEfficacyHandler instance.
        /// </summary>
        public static void SetDiscoveredCombinations(TreatmentEfficacyHandler handler, HashSet<string> combinations)
        {
            DiscoveredCombinationsField?.SetValue(handler, combinations);
        }

        /// <summary>
        ///     Invokes the private static MakeDiscoveryKey method.
        /// </summary>
        public static string InvokeMakeDiscoveryKey(string treatmentName, string afflictionName)
        {
            return MakeDiscoveryKeyMethod?.Invoke(null, new object[] { treatmentName, afflictionName }) as string;
        }

        /// <summary>
        ///     Invokes the private static SaveDiscoveryState method.
        /// </summary>
        public static void InvokeSaveDiscoveryState(HashSet<string> combinations)
        {
            SaveDiscoveryStateMethod?.Invoke(null, new object[] { combinations });
        }

        /// <summary>
        ///     Invokes the private static LoadDiscoveryData method.
        /// </summary>
        public static DiscoveryData InvokeLoadDiscoveryData()
        {
            return LoadDiscoveryDataMethod?.Invoke(null, null) as DiscoveryData;
        }

        /// <summary>
        ///     Invokes the private static DiscoveryDataExists method.
        /// </summary>
        public static bool InvokeDiscoveryDataExists()
        {
            return (bool)(DiscoveryDataExistsMethod?.Invoke(null, null) ?? false);
        }

        /// <summary>
        ///     Invokes the private MarkAsDiscovered method.
        /// </summary>
        /// <param name="handler">Handler instance to invoke on</param>
        /// <param name="treatmentName">Name of the treatment</param>
        /// <param name="afflictionName">Name of the affliction</param>
        /// <param name="existingEfficacy">Efficacy value (currently unused, reserved for future analytics)</param>
        public static void InvokeMarkAsDiscovered(TreatmentEfficacyHandler handler, string treatmentName,
            string afflictionName, int existingEfficacy)
        {
            MarkAsDiscoveredMethod?.Invoke(handler, new object[] { treatmentName, afflictionName, existingEfficacy });
        }
    }
}