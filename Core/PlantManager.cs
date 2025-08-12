using System;
using System.Collections.Generic;
using UnityEngine;

namespace _project.Scripts.Core
{
    public class PlantManager : MonoBehaviour
    {
        public readonly List<GameObject> CachedPlants = new();

        public void TriggerPlantTreatments()
        {
            if (CachedPlants == null)
            {
                Debug.LogWarning("CachedPlants is null, cannot trigger plant treatments");
                return;
            }

            foreach (var plant in CachedPlants)
            {
                if (!plant)
                {
                    Debug.LogWarning("Found null plant in CachedPlants, skipping");
                    continue;
                }

                var controller = plant.GetComponent<PlantController>();
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