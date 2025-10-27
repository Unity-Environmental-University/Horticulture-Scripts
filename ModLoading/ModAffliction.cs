using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<string, Func<PlantAfflictions.ITreatment>> LegacyTreatmentFactories =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["horticulturaloil"] = () => new PlantAfflictions.HorticulturalOilTreatment(),
                ["fungicide"] = () => new PlantAfflictions.FungicideTreatment(),
                ["insecticide"] = () => new PlantAfflictions.InsecticideTreatment(),
                ["soapywater"] = () => new PlantAfflictions.SoapyWaterTreatment(),
                ["spinosad"] = () => new PlantAfflictions.SpinosadTreatment(),
                ["imidacloprid"] = () => new PlantAfflictions.ImidaclopridTreatment(),
                ["panacea"] = () => new PlantAfflictions.Panacea()
            };

        private readonly string[] _vulnerableToTreatments;
        private bool _hasAdults = true;
        private bool _hasLarvae = true;
        
        public ModAffliction(string name, string description, Color color, string shaderName = null,
            string[] vulnerableToTreatments = null, bool isSpreadable = true)
        {
            Name = name ?? "Unknown Affliction";
            Description = description ?? "";
            Color = color;
            Shader = !string.IsNullOrEmpty(shaderName) ? Shader.Find(shaderName) : null;
            _vulnerableToTreatments = vulnerableToTreatments?.ToArray() ?? Array.Empty<string>();
            AcceptableTreatments = BuildAcceptableTreatments(_vulnerableToTreatments);
            IsSpreadable = isSpreadable;
        }

        public string Name { get; }
        public string Description { get; }
        public Color Color { get; }
        public Shader Shader { get; }
        public bool IsSpreadable { get; }
        public List<PlantAfflictions.ITreatment> AcceptableTreatments { get; }

        public PlantAfflictions.IAffliction Clone()
        {
            return new ModAffliction(Name, Description, Color, Shader?.name, _vulnerableToTreatments, IsSpreadable);
        }

        public bool TreatWith(PlantAfflictions.ITreatment treatment, PlantController plant)
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
                    infectReduction = treatment.InfectCureValue ?? 0;
                }

                if (currentEggs > 0)
                {
                    eggReduction = treatment.EggCureValue ?? 0;
                }

                // Apply reductions to plant using public method
                plant.ReduceAfflictionValues(this, infectReduction, eggReduction);

                // Update remaining status from plant values after treatment resolves
                var remainingInfect = plant.GetInfectFrom(this);
                var remainingEggs = plant.GetEggsFrom(this);
                _hasAdults = remainingInfect > 0;
                _hasLarvae = remainingEggs > 0;

                // Remove affliction if completely treated
                if (!_hasAdults && !_hasLarvae)
                {
                    plant.RemoveAffliction(this);
                }

                return true;
            }
            else if (CardGameMaster.Instance?.debuggingCardClass == true)
            {
                Debug.Log($"{treatment.Name} has no effect on {Name} (not vulnerable to this treatment type)");
            }

            return false;
        }

        public bool CanBeTreatedBy(PlantAfflictions.ITreatment treatment)
        {
            return IsVulnerableTo(treatment);
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

        private static List<PlantAfflictions.ITreatment> BuildAcceptableTreatments(IEnumerable<string> treatmentNames)
        {
            var result = new List<PlantAfflictions.ITreatment>();

            if (treatmentNames == null)
            {
                return result;
            }

            foreach (var originalName in treatmentNames)
            {
                if (string.IsNullOrWhiteSpace(originalName))
                {
                    continue;
                }

                var key = originalName.Replace(" ", string.Empty).ToLowerInvariant();
                if (!LegacyTreatmentFactories.TryGetValue(key, out var factory))
                {
                    continue;
                }

                result.Add(factory());
            }

            return result;
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
