using UnityEngine;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    ///     MonoBehaviour-based configuration that exposes the prefabs and materials needed by PlantHealthBarHandler.
    ///     Attach this component to the same GameObject as your PlantController (or a nearby parent) and wire it in prefabs.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlantHealthBarConfig : MonoBehaviour
    {
        [Header("Icon Prefabs")] 
        [Tooltip("Prefab used for infection/heart icons")]
        public GameObject heartPrefab;

        [Tooltip("Prefab used for egg icons")] public GameObject eggPrefab;

        [Header("Affliction Materials")] 
        [Tooltip("Material for Aphids infection icons")]
        public Material aphidsMaterial;

        [Tooltip("Material for MealyBugs infection icons")]
        public Material mealybugsMaterial;

        [Tooltip("Material for Thrips infection icons")]
        public Material thripsMaterial;

        [Tooltip("Material for Mildew infection icons")]
        public Material mildewMaterial;

        [Tooltip("Material for Fungus Gnats infection icons")]
        public Material gnatsMaterial;

        [Tooltip("Material for Spider Mites infection icons")]
        public Material spiderMitesMaterial;

        [Tooltip("Material for all egg icons")]
        public Material eggsMaterial;

#if UNITY_EDITOR
        private void OnValidate()
        {
            IsValid();
        }
#endif

        /// <summary>
        ///     Validates that all required materials and prefabs are assigned.
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise.</returns>
        public bool IsValid()
        {
            if (heartPrefab == null)
            {
                Debug.LogError("[PlantHealthBarConfig] Heart prefab is not assigned!", this);
                return false;
            }

            if (eggPrefab == null)
            {
                Debug.LogError("[PlantHealthBarConfig] Egg prefab is not assigned!", this);
                return false;
            }

            var missingMaterials = 0;
            if (aphidsMaterial == null)
            {
                Debug.LogWarning("[PlantHealthBarConfig] Aphids material is not assigned!", this);
                missingMaterials++;
            }

            if (mealybugsMaterial == null)
            {
                Debug.LogWarning("[PlantHealthBarConfig] MealyBugs material is not assigned!", this);
                missingMaterials++;
            }

            if (thripsMaterial == null)
            {
                Debug.LogWarning("[PlantHealthBarConfig] Thrips material is not assigned!", this);
                missingMaterials++;
            }

            if (mildewMaterial == null)
            {
                Debug.LogWarning("[PlantHealthBarConfig] Mildew material is not assigned!", this);
                missingMaterials++;
            }

            if (gnatsMaterial == null)
            {
                Debug.LogWarning("[PlantHealthBarConfig] Gnats material is not assigned!", this);
                missingMaterials++;
            }

            if (spiderMitesMaterial == null)
            {
                Debug.LogWarning("[PlantHealthBarConfig] Spider Mites material is not assigned!", this);
                missingMaterials++;
            }

            if (eggsMaterial != null) return missingMaterials == 0;
            Debug.LogWarning("[PlantHealthBarConfig] Eggs material is not assigned!", this);

            return false;
        }
    }
}