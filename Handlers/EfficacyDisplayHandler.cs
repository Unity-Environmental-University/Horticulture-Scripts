using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using TMPro;
using UnityEngine;

namespace _project.Scripts.Handlers
{
    public class EfficacyDisplayHandler : MonoBehaviour
    {
        // Declarations
        [SerializeField] private TextMeshPro efficacyText;
        [SerializeField] private PlantController plantController;
        private readonly List<PlantAfflictions.IAffliction> _afflictions = new();
        private PlantAfflictions.ITreatment _treatment;

        public void SetPlant(PlantController controller) => plantController = controller;
        public void SetTreatment(PlantAfflictions.ITreatment treatment) => _treatment = treatment;

        // ReSharper disable Unity.PerformanceAnalysis
        public void UpdateInfo()
        {
            var controller = TryGetPlantController();
            var treatment = TryGetTreatment();

            if (!controller || treatment == null || controller.CurrentAfflictions == null || controller.CurrentAfflictions.Count == 0)
            {
                UpdateDisplay(null, null);
                return;
            }

            var afflictions = TryGetAfflictions();
            if (afflictions == null || afflictions.Count == 0)
            {
                UpdateDisplay(null, treatment);
                return;
            }

            // Pass in only the first one rn, will need to make this check if there is a valid relationship at some point
            UpdateDisplay(afflictions.First(), treatment);
        }

        private PlantController TryGetPlantController()
        {
            if (plantController) return plantController;

            if (TryGetComponent(out PlantController controller))
            {
                plantController = controller;
                return plantController;
            }

            controller = GetComponentInParent<PlantController>(true);
            if (controller)
            {
                plantController = controller;
                return plantController;
            }

            controller = GetComponentInChildren<PlantController>(true);
            if (controller)
            {
                plantController = controller;
                return plantController;
            }

            var holder = GetComponentInParent<PlacedCardHolder>();
            if (!holder) return plantController;

            var searchRoot = holder.transform.parent ? holder.transform.parent : holder.transform;
            plantController = searchRoot.GetComponentInChildren<PlantController>(true);

            return plantController;
        }

        private PlantAfflictions.ITreatment TryGetTreatment()
        {
            if (_treatment != null) return _treatment;
            if (!TryGetComponent(out PlantAfflictions.ITreatment treatment)) return null;
            _treatment = treatment;
            return _treatment;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private List<PlantAfflictions.IAffliction> TryGetAfflictions()
        {
            _afflictions.Clear();

            var plant = TryGetPlantController();
            if (plant && plant.CurrentAfflictions is { Count: > 0 })
            {
                _afflictions.AddRange(plant.CurrentAfflictions);
                return _afflictions;
            }

            if (!TryGetComponent(out PlantAfflictions.IAffliction affliction) || affliction == null) return null;
            _afflictions.Add(affliction);
            return _afflictions;
        }

        private void UpdateDisplay(PlantAfflictions.IAffliction affliction, PlantAfflictions.ITreatment treatment)
        {
            if (!efficacyText)
            {
                Debug.LogWarning("EfficacyDisplayHandler requires a TextMeshPro reference.", this);
                return;
            }

            var handler = CardGameMaster.Instance?.treatmentEfficacyHandler;
            if (!handler || affliction == null || treatment == null)
            {
                efficacyText.text = string.Empty;
                return;
            }

            var efficacy = handler.GetRelationalEfficacy(affliction, treatment);
            efficacyText.text = efficacy + "%";
        }

        public void Clear()
        {
            _afflictions.Clear();
            _treatment = null;
            UpdateDisplay(null, null);
        }
    }
}
