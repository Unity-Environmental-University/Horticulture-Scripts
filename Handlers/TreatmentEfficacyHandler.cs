using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Handlers
{
    [Serializable]
    public class RelationalEfficacy
    {
        public int efficacy;

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
                existing.SetNames(affliction, treatment);
                return Mathf.Clamp(existing.efficacy, 0, 100);
            }

            var rel = new RelationalEfficacy
            {
                treatment = treatment,
                affliction = affliction,
                efficacy = Mathf.Clamp(treatment.Efficacy ?? DefaultEfficacy, 0, 100)
            };

            rel.SetNames(affliction, treatment);
            relationalEfficacys.Add(rel);
            return rel.efficacy;
        }
    }
}