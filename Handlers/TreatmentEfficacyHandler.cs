using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
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

        public int GetRelationalEfficacy(PlantAfflictions.IAffliction affliction, PlantAfflictions.ITreatment treatment)
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
                existing.interactionCount++;
                existing.SetNames(affliction, treatment);
                existing.TouchEfficacy();
                return Mathf.Clamp(existing.efficacy, 0, 100);
            }

            // Return Early if incompatible
            if (!affliction.CanBeTreatedBy(treatment)) return 0;
            var rel = new RelationalEfficacy
            {
                treatment = treatment,
                affliction = affliction,
                interactionCount = 1,
                efficacy = Mathf.Clamp(treatment.Efficacy ?? DefaultEfficacy, 0, 100)
            };

            rel.SetNames(affliction, treatment);
            relationalEfficacies.Add(rel);
            return rel.efficacy;
        }
    }
}
