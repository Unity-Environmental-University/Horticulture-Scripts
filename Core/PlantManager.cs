using System;
using System.Collections.Generic;
using UnityEngine;

namespace _project.Scripts.Core
{
    /// <summary>
    ///     Manages the collection of all plants in the scene and coordinates their daily processing.
    /// </summary>
    public class PlantManager : MonoBehaviour
    {
        public readonly List<GameObject> cachedPlants = new();

        /// <summary>
        ///     Processes daily activities for all managed plants. Called once per game turn.
        /// </summary>
        public void TriggerPlantTreatments()
        {
            if (cachedPlants == null)
            {
                Debug.LogError("CachedPlants is null, cannot trigger plant treatments");
                return;
            }

            foreach (var plant in cachedPlants)
            {
                if (!plant)
                {
                    Debug.LogWarning("Found null plant in CachedPlants, skipping");
                    continue;
                }

                plant.TryGetComponent<PlantController>(out var controller);
                if (!controller)
                {
                    Debug.LogWarning($"PlantController not found on {plant.name}, skipping");
                    continue;
                }

                try
                {
                    controller.ProcessDay();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing day for plant {plant.name}: {e.Message}");
                }
            }
        }
    }
}