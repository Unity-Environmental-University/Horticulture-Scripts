using System;
using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.ModLoading
{
    /// <summary>
    ///     Fully data-driven affliction that can be defined in mod files
    /// </summary>
    public class ModAffliction : PlantAfflictions.IAffliction
    {
        private readonly string[] _vulnerableToTreatments;
        private bool _hasAdults = true;
        private bool _hasLarvae = true;

        public ModAffliction(string name, string description, Color color, string shaderName = null,
            string[] vulnerableToTreatments = null)
        {
            Name = name ?? "Unknown Affliction";
            Description = description ?? "";
            Color = color;
            Shader = !string.IsNullOrEmpty(shaderName) ? Shader.Find(shaderName) : null;
            _vulnerableToTreatments = vulnerableToTreatments ?? Array.Empty<string>();
        }

        public string Name { get; }
        public string Description { get; }
        public Color Color { get; }
        public Shader Shader { get; }

        public PlantAfflictions.IAffliction Clone()
        {
            return new ModAffliction(Name, Description, Color, Shader?.name, _vulnerableToTreatments);
        }

        public void TreatWith(PlantAfflictions.ITreatment treatment, PlantController plant)
        {
            var infectReduction = 0;
            var eggReduction = 0;

            // Get current affliction levels from plant
            var currentInfect = plant.GetInfectFrom(this);
            var currentEggs = plant.GetEggsFrom(this);

            // Check if this treatment is effective against this mod affliction
            if (IsVulnerableTo(treatment))
            {
                if (currentInfect > 0)
                {
                    _hasAdults = false;
                    infectReduction = treatment.InfectCureValue ?? 0;
                }

                if (currentEggs > 0)
                {
                    _hasLarvae = false;
                    eggReduction = treatment.EggCureValue ?? 0;
                }

                // Apply reductions to plant using public method
                plant.ReduceAfflictionValues(this, infectReduction, eggReduction);

                // Remove affliction if completely treated
                if (!_hasAdults && !_hasLarvae) plant.RemoveAffliction(this);
            }
            else if (CardGameMaster.Instance?.debuggingCardClass == true)
            {
                Debug.Log($"{treatment.Name} has no effect on {Name} (not vulnerable to this treatment type)");
            }
        }

        public void TickDay(PlantController plant)
        {
            // Default implementation - can be overridden in derived classes
            // For now, mod afflictions don't spread/worsen over time
        }

        public ICard GetCard()
        {
            // Mod afflictions don't have associated cards by default
            return null;
        }

        private bool IsVulnerableTo(PlantAfflictions.ITreatment treatment)
        {
            // ModTreatments always work via affliction-specific effectiveness
            if (treatment is ModLoader.ModTreatment modTreatment)
            {
                var (infectCure, eggCure) = modTreatment.GetEffectivenessFor(Name);
                return infectCure > 0 || eggCure > 0;
            }

            // Legacy treatments work based on treatment name vulnerability
            var treatmentName = treatment.GetType().Name.Replace("Treatment", "");
            return _vulnerableToTreatments.Any(vulnerableTo =>
                string.Equals(vulnerableTo, treatmentName, StringComparison.OrdinalIgnoreCase));
        }
    }
}