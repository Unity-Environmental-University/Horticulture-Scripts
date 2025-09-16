using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Handlers
{
    public class RelationalEfficacy
    {
        public PlantAfflictions.IAffliction affliction;
        public PlantAfflictions.ITreatment treatment;
        public int efficacy;
    }

    public class TreatmentEfficacyHandler : MonoBehaviour
    {
        private readonly List<RelationalEfficacy> relationalEfficacies = new();

        public int GetRelationalEfficacy(PlantAfflictions.ITreatment treatment, PlantAfflictions.IAffliction affliction)
        {
            if (relationalEfficacies.Any(r => r.treatment == treatment && r.affliction == affliction))
            {
                var efficacy = relationalEfficacies
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
                efficacy = treatment.Efficacy.Value
            };
            relationalEfficacies.Add(rel);
            return rel.efficacy;
        }
    }
}