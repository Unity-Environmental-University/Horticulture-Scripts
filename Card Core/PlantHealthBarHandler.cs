using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class PlantHealthBarHandler : MonoBehaviour
    {
        [SerializeField] private PlantController plantController;
        public GameObject heartPrefab;
        public GameObject eggPrefab;
        public GameObject infectBarParent;
        public GameObject eggBarParent;

        private void Start()
        {
            plantController = GetComponentInParent<PlantController>();

            if (!plantController) return;

            SpawnHearts(plantController);
        }

        public void SpawnHearts(PlantController plant)
        {
            var infLevel = plant.GetInfectLevel();
            var eggLevel = plant.GetEggLevel();


            if (!infectBarParent || !eggBarParent) return;
            for (var i = 0; i < infLevel; i++)
                Instantiate(heartPrefab, infectBarParent.transform, false);

            for (var i = 0; i < eggLevel; i++)
                Instantiate(eggPrefab, eggBarParent.transform, false);
        }
    }
}