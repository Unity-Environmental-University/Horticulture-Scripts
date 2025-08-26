using _project.Scripts.Core;
using TMPro;
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
        [SerializeField] private TextMeshPro infectBreakdownText;
        [SerializeField] private TextMeshPro eggBreakdownText;

        private void Start()
        {
            plantController = GetComponentInParent<PlantController>();

            if (!plantController) return;

            SpawnHearts(plantController);
            // UpdateBreakdowns();
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

        // private void UpdateBreakdowns()
        // {
        //     if (!plantController || plantController.PlantCard is not IPlantCard card) return;
        //
        //     if (infectBreakdownText)
        //     {
        //         var parts = (from kv in card.Infect.All where kv.Value.infect > 0 select $"{kv.Key}: {kv.Value.infect}")
        //             .ToList();
        //         infectBreakdownText.text = parts.Count > 0 ? string.Join("\n", parts) : string.Empty;
        //     }
        //
        //     if (!eggBreakdownText) return;
        //     {
        //         var parts = (from kv in card.Infect.All where kv.Value.eggs > 0 select $"{kv.Key}: {kv.Value.eggs}")
        //             .ToList();
        //         eggBreakdownText.text = parts.Count > 0 ? string.Join("\n", parts) : string.Empty;
        //     }
        // }
    }
}