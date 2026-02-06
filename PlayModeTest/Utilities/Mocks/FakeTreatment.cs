using System;
using System.Collections.Generic;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.PlayModeTest.Utilities.Mocks
{
    /// <summary>
    ///     Fake treatment for testing that cures all afflictions.
    /// </summary>
    public class FakeTreatment : PlantAfflictions.ITreatment
    {
        public string Name { get; set; } = "Panacea";
        public string Description { get; set; } = "Cures all afflictions";
        public bool IsSynthetic { get; set; } = true;
        public int? InfectCureValue { get; set; } = 999;
        public int? EggCureValue { get; set; } = 0;
        public int? Efficacy { get; set; } = 100;

        public void ApplyTreatment(PlantController plant)
        {
            var afflictions = plant.CurrentAfflictions != null
                ? new List<PlantAfflictions.IAffliction>(plant.CurrentAfflictions)
                : new List<PlantAfflictions.IAffliction>();

            // Apply cure values before removing afflictions
            foreach (var affliction in afflictions)
            {
                var infectCure = InfectCureValue ?? 0;
                var eggCure = EggCureValue ?? 0;
                if (infectCure > 0 || eggCure > 0) plant.ReduceAfflictionValues(affliction, infectCure, eggCure);
            }

            // Remove all afflictions
            foreach (var affliction in afflictions) plant.RemoveAffliction(affliction);
        }
    }

    /// <summary>
    ///     Treatment that intentionally throws an exception for error handling tests.
    /// </summary>
    public class ThrowingTreatment : PlantAfflictions.ITreatment
    {
        public string Name => "Explosive";
        public string Description => "Throws on apply";
        public bool IsSynthetic { get; }
        public int? InfectCureValue { get; set; } = 0;
        public int? EggCureValue { get; set; } = 0;
        public int? Efficacy { get; set; } = 100;

        public void ApplyTreatment(PlantController plant)
        {
            Debug.LogException(new Exception("Intentional test exception"));
        }
    }

    /// <summary>
    ///     Treatment that clears the affliction list during iteration for list-modification tests.
    /// </summary>
    public class SelfClearingTreatment : PlantAfflictions.ITreatment
    {
        public string Name => "SafeClear";
        public string Description => "Removes all afflictions";
        public bool IsSynthetic { get; }
        public int? InfectCureValue { get; set; } = 999;
        public int? EggCureValue { get; set; } = 999;
        public int? Efficacy { get; set; } = 100;

        public void ApplyTreatment(PlantController plant)
        {
            var afflictionsCopy = new List<PlantAfflictions.IAffliction>(plant.CurrentAfflictions);

            // Apply cure values before removing afflictions
            foreach (var affliction in afflictionsCopy)
            {
                var infectCure = InfectCureValue ?? 0;
                var eggCure = EggCureValue ?? 0;
                if (infectCure > 0 || eggCure > 0) plant.ReduceAfflictionValues(affliction, infectCure, eggCure);
            }

            // Remove all afflictions
            foreach (var affliction in afflictionsCopy) plant.RemoveAffliction(affliction);
        }
    }
}