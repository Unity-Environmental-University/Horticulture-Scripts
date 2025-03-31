using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _project.Scripts.Core
{
    public class PlantManager : MonoBehaviour
    {
        public readonly List<GameObject> CachedPlants = new();

        public void TriggerPlantTreatments()
        {
            foreach (var controller in CachedPlants.Select(plant => plant.GetComponent<PlantController>()))
                controller.ProcessDay();
        }
    }
}