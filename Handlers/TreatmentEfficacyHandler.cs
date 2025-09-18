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
        public PlantAfflictions.IAffliction affliction;
        public PlantAfflictions.ITreatment treatment;
        public int efficacy;
        
        public string afflictionName;
        public string treatmentName;
        public string SetNames(PlantAfflictions.IAffliction a, PlantAfflictions.ITreatment T)
        {
            afflictionName = a.Name;
            treatmentName = T.Name;
            return $"{treatmentName} - {afflictionName}";
        }
    }

    public class TreatmentEfficacyHandler : MonoBehaviour
    {
        [SerializeField] private List<RelationalEfficacy> relationalEfficacys = new();
        private const int DefaultEfficacy = 100;

        public int GetRelationalEfficacy(PlantAfflictions.IAffliction affliction, PlantAfflictions.ITreatment treatment)
        {
            if (relationalEfficacys.Any(r => r.treatment == treatment && r.affliction == affliction))
            {
                var efficacy = relationalEfficacys
                    .FirstOrDefault(r => r.treatment == treatment && r.affliction == affliction)
                    ?.efficacy;
                if (efficacy != null)
                    return (int)efficacy;
            }

            // ReSharper disable once PossibleInvalidOperationException
            var rel = new RelationalEfficacy
            {
                treatment = treatment,
                affliction = affliction,
                efficacy = Mathf.Clamp(treatment.Efficacy ?? DefaultEfficacy,0,100)
            };
            rel.SetNames(affliction, treatment);
            relationalEfficacys.Add(rel);
            return rel.efficacy;
        }
    }
}