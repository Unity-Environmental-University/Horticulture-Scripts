using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
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
        [SerializeField] private List<RelationalEfficacy> relationalEfficacies = new();
        private HashSet<string> discoveredCombinations = new();

        [SerializeField] private bool discoveryModeEnabled = true;

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

        /// <summary>
        /// Creates a unique discovery key for a treatment-affliction combination.
        /// Uses pipe delimiter (|) which should not appear in treatment or affliction names.
        /// </summary>
        /// <remarks>
        /// Format: "TreatmentName|AfflictionName"
        /// Example: "Permethrin Spray|Spider Mites"
        /// </remarks>
        private string MakeDiscoveryKey(string treatmentName, string afflictionName)
            => $"{treatmentName}|{afflictionName}";

        public bool IsDiscovered(string treatmentName, string afflictionName)
        {
            // If discovery mode is disabled, all combinations are considered "discovered"
            if (!discoveryModeEnabled) return true;

            var key = MakeDiscoveryKey(treatmentName, afflictionName);
            return discoveredCombinations.Contains(key);
        }

        private void MarkAsDiscovered(string treatmentName, string afflictionName, int efficacy)
        {
            var key = MakeDiscoveryKey(treatmentName, afflictionName);
            if (discoveredCombinations.Add(key))
            {
                // Fire analytics event only on first discovery
                Analytics.AnalyticsFunctions.RecordEfficacyDiscovery(treatmentName, afflictionName, efficacy);
            }
        }

        public List<string> GetDiscoveredCombinationsForSave()
            => discoveredCombinations.ToList();

        public void RestoreDiscoveredCombinations(List<string> saved)
        {
            discoveredCombinations.Clear();
            if (saved != null)
                foreach (var key in saved)
                    discoveredCombinations.Add(key);
        }

        /// <summary>
        /// Gets or sets whether discovery mode is enabled.
        /// When disabled, all treatment efficacy percentages are visible immediately.
        /// </summary>
        public bool DiscoveryModeEnabled
        {
            get => discoveryModeEnabled;
            set => discoveryModeEnabled = value;
        }

        /// <summary>
        /// Clears all discovered treatment-affliction combinations.
        /// Useful for players who want to restart their learning progress.
        /// </summary>
        public void ClearAllDiscoveries()
        {
            discoveredCombinations.Clear();
            Debug.Log("[TreatmentEfficacyHandler] All discoveries cleared. Player will need to rediscover all combinations.");
        }

        /// <summary>
        /// Returns the total number of unique treatment-affliction combinations discovered.
        /// </summary>
        public int GetDiscoveryCount() => discoveredCombinations.Count;
    }
}
