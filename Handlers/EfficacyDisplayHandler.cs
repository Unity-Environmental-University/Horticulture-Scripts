using System.Collections.Generic;
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
        private List<PlantAfflictions.IAffliction> _afflictions;
        private PlantAfflictions.ITreatment _treatment;

        public void UpdateInfo()
        {
            var controller = TryGetPlantController();
            var treatment = TryGetTreatment();


            if (controller != null && treatment != null)
            {
                var afflictions = TryGetAfflictions();

                // Pass in only the first one rn, will need to make this check if there is a valid relationship at some point
                UpdateDisplay(afflictions[0], treatment);
            }
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
            var plant = plantController;
            if (plant != null)
                if (plant.CurrentAfflictions.Count > 0)
                {
                    foreach (var aff in plant.CurrentAfflictions) _afflictions.Add(aff);
                    return _afflictions;
                }

            if (!TryGetComponent(out PlantAfflictions.IAffliction affliction)) return null;
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
            _afflictions = null;
            _treatment = null;
            UpdateDisplay(null, null);
        }
    }
}