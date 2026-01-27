using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _project.Scripts.Handlers
{
    [Serializable]
    public class RelationalEfficacy
    {
        // IPM resistance thresholds - treatment becomes less effective with repeated use
        private const int HeavyInteractionThreshold = 15;
        private const int MediumInteractionThreshold = 10;
        private const int MildInteractionThreshold = 5;

        private const int HeavyResistanceDecayChance = 30;  // 30% at heavy use
        private const int MediumResistanceDecayChance = 50; // 50% at medium use
        private const int MildResistanceDecayChance = 80;   // 80% at mild use

        private const int EfficacyDecayAmount = 10;
        private const int MinimumEfficacy = 1;

        public int efficacy;
        public int interactionCount;
        public string afflictionName;
        public string treatmentName;
        public PlantAfflictions.IAffliction affliction;
        public PlantAfflictions.ITreatment treatment;

        public string SetNames(PlantAfflictions.IAffliction a, PlantAfflictions.ITreatment T)
        {
            afflictionName = a.Name;
            treatmentName = T.Name;
            return $"{treatmentName} - {afflictionName}";
        }

        public void TouchEfficacy()
        {
            var touchLevel = interactionCount switch
            {
                > HeavyInteractionThreshold => HeavyResistanceDecayChance,
                > MediumInteractionThreshold => MediumResistanceDecayChance,
                > MildInteractionThreshold => MildResistanceDecayChance,
                _ => (int?)null
            };

            if (!touchLevel.HasValue) return;

            RollEfficacyDecrease(touchLevel.Value);
        }

        private void RollEfficacyDecrease(int decayChancePercentage)
        {
            var chance = Random.Range(0, 100);
            if (chance < decayChancePercentage)
                efficacy = Mathf.Max(MinimumEfficacy, efficacy - EfficacyDecayAmount);
        }
    }

    public class TreatmentEfficacyHandler : MonoBehaviour
    {
        private const int DefaultEfficacy = 100;
        private readonly HashSet<string> discoveredCombinations = new();
        [SerializeField] private List<RelationalEfficacy> relationalEfficacies = new();
        [SerializeField] private bool discoveryModeEnabled = true;

        private void Awake()
        {
            discoveryModeEnabled =
                PlayerPrefs.GetInt(UserQualitySettings.DiscoveryModePrefKey, discoveryModeEnabled ? 1 : 0) == 1;
        }
        
        public int GetRelationalEfficacy(
            PlantAfflictions.IAffliction affliction,
            PlantAfflictions.ITreatment treatment,
            bool countInteraction = true)
        {
            if (affliction == null || treatment == null)
            {
                Debug.LogWarning("TreatmentEfficacyHandler: Cannot get efficacy for null affliction or treatment.");
                return 0;
            }

            var afflictionName = affliction.Name;
            var treatmentName = treatment.Name;

            var existing = relationalEfficacies.FirstOrDefault(r =>
                (string.Equals(r.afflictionName, afflictionName, StringComparison.Ordinal) &&
                 string.Equals(r.treatmentName, treatmentName, StringComparison.Ordinal)) ||
                (r.affliction == affliction && r.treatment == treatment));

            if (existing != null)
            {
                existing.affliction = affliction;
                existing.treatment = treatment;
                existing.SetNames(affliction, treatment);
                if (!countInteraction) return Mathf.Clamp(existing.efficacy, 0, 100);
                MarkAsDiscovered(treatmentName, afflictionName, existing.efficacy);
                existing.interactionCount++;
                existing.TouchEfficacy();
                return Mathf.Clamp(existing.efficacy, 0, 100);
            }

            // Return Early if incompatible
            if (!affliction.CanBeTreatedBy(treatment)) return 0;

            var baseEfficacy = Mathf.Clamp(treatment.Efficacy ?? DefaultEfficacy, 0, 100);
            if (!countInteraction) return baseEfficacy;

            var rel = new RelationalEfficacy
            {
                treatment = treatment,
                affliction = affliction,
                interactionCount = 1,
                efficacy = baseEfficacy
            };

            rel.SetNames(affliction, treatment);
            MarkAsDiscovered(treatmentName, afflictionName, baseEfficacy);
            relationalEfficacies.Add(rel);
            return rel.efficacy;
        }

        /// <summary>
        ///     Calculates the average treatment efficacy across all treatable afflictions on a plant.
        /// </summary>
        /// <param name="treatment">The treatment to evaluate</param>
        /// <param name="controller">The plant controller with afflictions to check</param>
        /// <returns>
        ///     Average efficacy percentage (0-100) across treatable afflictions only.
        ///     Returns 0 if no afflictions can be treated.
        /// </returns>
        /// <remarks>
        ///     Incompatible afflictions are filtered out before averaging to prevent misleading low percentages.
        ///     Uses preview mode (countInteraction: false) to avoid mutating resistance state during display.
        /// </remarks>
        public int GetAverageEfficacy(PlantAfflictions.ITreatment treatment, PlantController controller)
        {
            var afflictions = controller.CurrentAfflictions;
            if (afflictions == null || afflictions.Count == 0) return 0;

            // Filter only treatable afflictions to avoid including 0% incompatible results
            var treatableAfflictions = afflictions
                .Where(af => af.CanBeTreatedBy(treatment))
                .ToList();

            if (treatableAfflictions.Count == 0) return 0;

            // Use countInteraction: false to avoid mutating resistance state during preview
            var efficacies = treatableAfflictions
                .Select(af => GetRelationalEfficacy(af, treatment, false))
                .ToList();

            return (int)efficacies.Average();
        }
        
        private static string MakeDiscoveryKey(string treatmentName, string afflictionName)
        {
            return $"{treatmentName}|{afflictionName}";
        }
        
        private void MarkAsDiscovered(string treatmentName, string afflictionName, int existingEfficacy)
        {
            var key = MakeDiscoveryKey(treatmentName, afflictionName);
            if (discoveredCombinations.Add(key))
            {
                // Record Treatment Discovery event
                //Analytics.AnalyticsFunctions.RecordEfficacyDiscovery(treatmentName, afflictionName, efficacy);
            }
        }

        public bool IsDiscovered(string treatmentName, string afflictionName)
        {
            if (!discoveryModeEnabled) return true;
            
            var key = MakeDiscoveryKey(treatmentName, afflictionName);
            return discoveredCombinations.Contains(key);
        }

        /// <summary>
        ///     Gets or Sets whether Discovery Mode is enabled
        /// </summary>
        public bool DiscoveryModeEnabled
        {
            get => discoveryModeEnabled;
            set => discoveryModeEnabled = value;
        }

        /// <summary>
        ///     Clears all discovered combinations, resetting the player's progress in discovery mode.
        /// </summary>
        public void ClearDiscoveredCombinations()
        {
            discoveredCombinations.Clear();
            Debug.Log(
                "[TreatmentEfficacyHandler] All discoveries cleared. Player will need to rediscover all combinations.");
        }
    }
}
