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

        public void UpdateInfo()
        {
            var controller = TryGetPlantController();
            var treatment = TryGetTreatment();

            if (controller == null || treatment == null)
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
            if (plantController != null) return plantController;
            if (TryGetComponent(out PlantController controller)) plantController = controller;

            return plantController;
        }

        private PlantAfflictions.ITreatment TryGetTreatment()
        {
            if (!TryGetComponent(out PlantAfflictions.ITreatment treatment)) return null;
            _treatment = treatment;
            return _treatment;
        }

        private List<PlantAfflictions.IAffliction> TryGetAfflictions()
        {
            _afflictions.Clear();

            var plant = TryGetPlantController();
            if (plant != null && plant.CurrentAfflictions is { Count: > 0 })
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
            if (efficacyText == null)
            {
                Debug.LogWarning("EfficacyDisplayHandler requires a TextMeshPro reference.", this);
                return;
            }

            var handler = CardGameMaster.Instance?.treatmentEfficacyHandler;
            if (handler == null || affliction == null || treatment == null)
            {
                efficacyText.text = string.Empty;
                return;
            }

            var efficacy = handler.GetRelationalEfficacy(affliction, treatment);
            efficacyText.text = efficacy.ToString();
        }

        public void Clear()
        {
            _afflictions.Clear();
            _treatment = null;
            UpdateDisplay(null, null);
        }
    }
}