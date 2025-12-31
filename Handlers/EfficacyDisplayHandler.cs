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
        [SerializeField] private TextMeshPro efficacyText;
        [SerializeField] private PlantController plantController;
        private readonly List<PlantAfflictions.IAffliction> _afflictions = new();
        private PlantAfflictions.ITreatment _treatment;
        private TreatmentEfficacyHandler _efficacyHandler;
        private bool _hasInitialized;

        private void Awake() => CacheReferences();

        private void CacheReferences()
        {
            if (_hasInitialized) return;

            if (CardGameMaster.Instance) _efficacyHandler = CardGameMaster.Instance.treatmentEfficacyHandler;

            if (!plantController) plantController = FindPlantController();

            _hasInitialized = true;
        }

        public void SetPlant(PlantController controller) => plantController = controller;
        public void SetTreatment(PlantAfflictions.ITreatment treatment) => _treatment = treatment;

        public void UpdateInfo()
        {
            if (!_hasInitialized) CacheReferences();

            var controller = plantController ? plantController : FindPlantController();
            var treatment = GetTreatment();

            if (!controller || treatment == null || controller.CurrentAfflictions == null ||
                controller.CurrentAfflictions.Count == 0)
            {
                UpdateDisplay(null, null);
                return;
            }

            var afflictions = GetCurrentAfflictions(controller);
            if (afflictions == null || afflictions.Count == 0)
            {
                UpdateDisplay(null, treatment);
                return;
            }

            foreach (var affliction in afflictions)
            {
                //TODO: this sucks.. maybe unfuck this? Or don't... I'm not your dad.
                switch (treatment)
                {
                    case PlantAfflictions.HorticulturalOilTreatment when plantController.GetEggsFrom(affliction) <= 0:
                    case PlantAfflictions.InsecticideTreatment when plantController.GetInfectFrom(affliction) <= 0:
                        UpdateDisplay(affliction, treatment, "0%", Color.red);
                        return;
                }

                UpdateDisplay(affliction, treatment);
                if (affliction.CanBeTreatedBy(treatment)) return;
            }
        }

        private PlantController FindPlantController()
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

        private PlantAfflictions.ITreatment GetTreatment()
        {
            if (_treatment != null) return _treatment;
            TryGetComponent(out PlantAfflictions.ITreatment treatment);
            return treatment;
        }

        private List<PlantAfflictions.IAffliction> GetCurrentAfflictions(PlantController plant)
        {
            _afflictions.Clear();

            if (plant && plant.CurrentAfflictions is { Count: > 0 })
            {
                _afflictions.AddRange(plant.CurrentAfflictions);
                return _afflictions;
            }

            if (!TryGetComponent(out PlantAfflictions.IAffliction affliction) || affliction == null) return null;
            _afflictions.Add(affliction);
            return _afflictions;
        }

        private void UpdateDisplay(PlantAfflictions.IAffliction affliction, PlantAfflictions.ITreatment treatment,
            string overrideText = null, Color overrideColor = default)
        {
            if (!efficacyText)
            {
                Debug.LogWarning("EfficacyDisplayHandler requires a TextMeshPro reference.", this);
                return;
            }

            var handler = _efficacyHandler ? _efficacyHandler : CardGameMaster.Instance?.treatmentEfficacyHandler;

            if (!handler || affliction == null || treatment == null)
            {
                efficacyText.text = string.Empty;
                efficacyText.color = Color.white;
                return;
            }

            var efficacy = handler.GetRelationalEfficacy(affliction, treatment, false);
            var efficacyColor = efficacy switch{ < 50 => Color.red, < 75 => Color.yellow, _ => Color.green };
            efficacyText.text = efficacy + "%";
            efficacyText.color = efficacyColor;

            if (overrideText == null) return;
            efficacyText.text = overrideText;
            efficacyText.color = overrideColor;
        }
        
        public void Clear()
        {
            _afflictions.Clear();
            _treatment = null;
            UpdateDisplay(null, null);
        }
    }
}