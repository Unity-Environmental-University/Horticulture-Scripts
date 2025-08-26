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
        private int _lastEggCount = -1;
        private int _lastInfectCount = -1;

        private void Start()
        {
            plantController = GetComponentInParent<PlantController>();

            if (!plantController) return;

            SpawnHearts(plantController);
        }

        private void Update()
        {
            if (!plantController) return;
            var currentInf = plantController.GetInfectLevel();
            var currentEgg = plantController.GetEggLevel();

            if (currentInf == _lastInfectCount && currentEgg == _lastEggCount) return;
            // Sync UI to new values
            SyncBar(infectBarParent, heartPrefab, currentInf, ref _lastInfectCount);
            SyncBar(eggBarParent, eggPrefab, currentEgg, ref _lastEggCount);
        }

        public void SpawnHearts(PlantController plant)
        {
            var infLevel = plant.GetInfectLevel();
            var eggLevel = plant.GetEggLevel();
            SyncBar(infectBarParent, heartPrefab, infLevel, ref _lastInfectCount);
            SyncBar(eggBarParent, eggPrefab, eggLevel, ref _lastEggCount);
        }

        private void SyncBar(GameObject barParent, GameObject prefab, int targetCount, ref int cache)
        {
            if (!barParent || !prefab) return;
            var t = barParent.transform;
            var current = t.childCount;

            if (current < targetCount)
                for (var i = current; i < targetCount; i++)
                    Instantiate(prefab, t, false);
            else if (current > targetCount)
                for (var i = current - 1; i >= targetCount; i--)
                {
                    var child = t.GetChild(i);
                    if (child) Destroy(child.gameObject);
                }

            cache = targetCount;
        }
    }
}