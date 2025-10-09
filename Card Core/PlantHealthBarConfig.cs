using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    ///     MonoBehaviour-based configuration that exposes the prefabs and materials needed by PlantHealthBarHandler.
    ///     Attach this component to the same GameObject as your PlantController (or a nearby parent) and wire it in prefabs.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlantHealthBarConfig : MonoBehaviour
    {
        [Header("Icon Prefabs")] [Tooltip("Prefab used for infection/heart icons")]
        public GameObject heartPrefab;

        [Tooltip("Prefab used for egg icons")] public GameObject eggPrefab;

        [Tooltip("Material for all egg icons")]
        public Material eggsMaterial;

        [Header("Affliction Materials")]
        [Tooltip("Maps affliction names to materials for health bar icons. Required entries: Aphids, MealyBugs, Thrips, Mildew, Fungus Gnats, Spider Mites.")]
        [SerializeField]
        private List<AfflictionMaterialMapping> afflictionMaterials = new();

#if UNITY_EDITOR
        private void OnValidate()
        {
            IsValid();
        }
#endif

        /// <summary>
        ///     Validates that all required materials and prefabs are assigned.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise.</returns>
        public bool IsValid()
        {
            var ok = true;
            if (!heartPrefab || !eggPrefab || !eggsMaterial)
            {
                Debug.LogWarning("[PlantHealthBarConfig] Required prefabs not assigned.", this);
                ok = false;
            }

            // Validate affliction materials list (optional)
            if (afflictionMaterials is not { Count: > 0 }) return ok;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in afflictionMaterials)
            {
                if (string.IsNullOrWhiteSpace(m.afflictionName))
                {
                    Debug.LogWarning("[PlantHealthBarConfig] AfflictionMaterials contains an entry with an empty name.",
                        this);
                    continue;
                }

                if (!seen.Add(m.afflictionName))
                    Debug.LogWarning($"[PlantHealthBarConfig] Duplicate affliction name '{m.afflictionName}' in list.",
                        this);
                if (!m.material)
                    Debug.LogWarning($"[PlantHealthBarConfig] Material not assigned for '{m.afflictionName}'.", this);
            }

            return ok;
        }

        /// <summary>
        ///     Builds an affliction name -> material map from the configured affliction materials list.
        /// </summary>
        public Dictionary<string, Material> BuildAfflictionMaterialMap()
        {
            // Use case-sensitive matching to enforce consistent affliction naming conventions
            var map = new Dictionary<string, Material>(StringComparer.Ordinal);

            if (afflictionMaterials == null) return map;
            foreach (var m in afflictionMaterials.Where(m =>
                         !string.IsNullOrWhiteSpace(m.afflictionName) && m.material))
                map[m.afflictionName] = m.material;

            return map;
        }

        /// <summary>
        ///     Maps an affliction name to its corresponding material for health bar icon rendering.
        /// </summary>
        [Serializable]
        public class AfflictionMaterialMapping
        {
            /// <summary>
            ///     The name of the affliction (e.g., "Aphids", "Thrips", "Spider Mites").
            ///     Must exactly match the Name property of the affliction class (case-sensitive).
            /// </summary>
            [Tooltip("Affliction name must exactly match the affliction's Name property (case-sensitive). Example: 'Aphids', 'Spider Mites'")]
            public string afflictionName;

            /// <summary>
            ///     The material to apply to health bar icons for this affliction type.
            /// </summary>
            [Tooltip("Material displayed on infection icons for this affliction")]
            public Material material;
        }
    }
}