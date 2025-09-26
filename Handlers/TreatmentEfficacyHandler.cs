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
        private const int HeavyInteractionThreshold = 15;
        private const int MediumInteractionThreshold = 10;
        private const int MildInteractionThreshold = 5;
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
                > HeavyInteractionThreshold => 30,
                > MediumInteractionThreshold => 50,
                > MildInteractionThreshold => 80,
                _ => (int?)null
            };

            if (!touchLevel.HasValue) return;

            RollEfficacyDecrease(touchLevel.Value);
        }

        private void RollEfficacyDecrease(int touchLevel)
        {
            var chance = Random.Range(0, 100);
            if (chance < touchLevel)
                efficacy = Mathf.Max(1, efficacy - 10);
        }
    }

    public class TreatmentEfficacyHandler : MonoBehaviour
    {
        private const int DefaultEfficacy = 100;
        [SerializeField] private List<RelationalEfficacy> relationalEfficacys = new();

        public int GetRelationalEfficacy(PlantAfflictions.IAffliction affliction, PlantAfflictions.ITreatment treatment)
        {
            var afflictionName = affliction.Name;
            var treatmentName = treatment.Name;

            var existing = relationalEfficacys.FirstOrDefault(r =>
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
            relationalEfficacys.Add(rel);
            return rel.efficacy;
        }
    }
}