using System.Collections.Generic;
using _project.Scripts.Card_Core;
using _project.Scripts.Core;
using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable UnusedMember.Global

namespace _project.Scripts.Classes
{
    public class PlantAfflictions : MonoBehaviour
    {
        // Constants for treatment effectiveness
        private const int StandardCureAmount = 1;
        private const int PanaceaCureAmount = 999;
        private const int DefaultEfficacy = 100;

        public interface IAffliction
        {
            string Name { get; }
            string Description { get; }
            public Color Color { get; }
            [CanBeNull] public Shader Shader { get; }
            public void TreatWith(ITreatment treatment, PlantController plant);
            public void TickDay(PlantController plant);

            [CanBeNull]
            public ICard GetCard() { return null; }

            public IAffliction Clone();
        }

        public interface ITreatment
        {
            string Name { get; }
            string Description { get; }
            int? InfectCureValue { get; set; }
            int? EggCureValue { get; set; }
            int? Efficacy { get; set; }

            public void ApplyTreatment(PlantController plant)
            {
                if (!plant)
                {
                    Debug.LogWarning("PlantController is null, cannot apply treatment.");
                    return;
                }

                var afflictions = plant.CurrentAfflictions != null
                    ? new List<IAffliction>(plant.CurrentAfflictions)
                    : new List<IAffliction>();
                
                if (afflictions.Count == 0) Debug.LogWarning("No afflictions found on the plant.");

                foreach (var item in afflictions)
                {
                    item.TreatWith(this, plant);
                    if (CardGameMaster.Instance == null || !CardGameMaster.Instance.debuggingCardClass) continue;
                    if (CardGameMaster.Instance.debuggingCardClass)
                        Debug.Log($"Applied treatment to affliction: {item.Name}");
                }
            }
        }

        #region IAfflictions

        public class ThripsAffliction : IAffliction
        {
            private bool _hasAdults = true;
            private bool _hasLarvae = true;
            public string Name => "Thrips";
            public string Description => "";
            public Color Color => Color.black;
            public Shader Shader => Shader.Find($"Shader Graphs/Thrips");

            public IAffliction Clone() { return new ThripsAffliction(); }

            public void TreatWith(ITreatment treatment, PlantController plant)
            {
                var infectReduction = 0;
                var eggReduction = 0;
                
                // Get actual current values from plant (not internal flags)
                var currentInfect = plant.GetInfectFrom(this);
                var currentEggs = plant.GetEggsFrom(this);
                
                // Insecticide or Panacea: targets adults (reduces infect)
                if (treatment is InsecticideTreatment or Panacea)
                {
                    if (currentInfect > 0) // Only treat if actual infect exists
                    {
                        _hasAdults = false;
                        infectReduction = treatment.InfectCureValue ?? 0;
                    }
                }
                
                // Horticultural Oil or Panacea: targets larvae (reduces eggs)
                if (treatment is HorticulturalOilTreatment or Panacea)
                {
                    if (currentEggs > 0) // Only treat if actual eggs exist
                    {
                        _hasLarvae = false;
                        eggReduction = treatment.EggCureValue ?? 0;
                    }
                }

                if (infectReduction > 0 || eggReduction > 0)
                {
                    plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                }
                
                // Update internal flags based on remaining values after treatment
                var remainingInfect = plant.GetInfectFrom(this);
                var remainingEggs = plant.GetEggsFrom(this);
                _hasAdults = remainingInfect > 0;
                _hasLarvae = remainingEggs > 0;
            }

            public void TickDay(PlantController plant)
            {
                if (!plant.PlantCard.Value.HasValue) return;
                var newVal = Mathf.Max(0, plant.PlantCard.Value.Value - 1);
                plant.PlantCard.Value = newVal;
                plant.UpdatePriceFlag(newVal);
            }


            public ICard GetCard() { return new ThripsCard(); }
        }

        public class MealyBugsAffliction : IAffliction
        {
            public string Name => "MealyBugs";
            public string Description => "";
            public Color Color => Color.red;
            public Shader Shader => Shader.Find($"Shader Graphs/MealyBugs");

            public IAffliction Clone() { return new MealyBugsAffliction(); }

            public void TreatWith(ITreatment treatment, PlantController plant)
            {
                if (treatment is SoapyWaterTreatment or InsecticideTreatment or ImidaclopridTreatment or Panacea)
                {
                    var chance = CardGameMaster.Instance.treatmentEfficacyHandler.GetRelationalEfficacy(this, treatment);
                    Debug.Log($"Treatment Efficacy: {chance}");
                    if (Random.Range(0, 100) < chance)
                    {
                        var infectReduction = treatment.InfectCureValue ?? 0;
                        var eggReduction = treatment.EggCureValue ?? 0;
                        plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                        Debug.Log("Treatment Successful");
                    }
                    else{Debug.Log("Failed to treat with MealyBugs");}
                }
            }

            public void TickDay(PlantController plant)
            {
                if (!plant.PlantCard.Value.HasValue) return;
                var newVal = Mathf.Max(0, plant.PlantCard.Value.Value - 1);
                plant.PlantCard.Value = newVal;
                plant.UpdatePriceFlag(newVal);
            }
            public ICard GetCard() { return new MealyBugsCard(); }
        }

        public class MildewAffliction : IAffliction
        {
            public string Name => "Mildew";
            public string Description => "";
            public Color Color => Color.white;
            public Shader Shader => Shader.Find($"Shader Graphs/Mold");

            public IAffliction Clone() { return new MildewAffliction(); }

            public void TreatWith(ITreatment treatment, PlantController plant)
            {
                if (treatment is FungicideTreatment or Panacea)
                {
                    var infectReduction = treatment.InfectCureValue ?? 0;
                    var eggReduction = treatment.EggCureValue ?? 0;
                    plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                }
            }

            public void TickDay(PlantController plant)
            {
                if (!plant.PlantCard.Value.HasValue) return;
                var newVal = Mathf.Max(0, plant.PlantCard.Value.Value - 1);
                plant.PlantCard.Value = newVal;
                plant.UpdatePriceFlag(newVal);
            }
            public ICard GetCard() { return new MildewCard(); }
        }

        public class AphidsAffliction : IAffliction
        {
            public string Name => "Aphids";
            public string Description => "";
            public Color Color => Color.cyan;
            public Shader Shader => Shader.Find($"Shader Graphs/Aphids");
            public IAffliction Clone() { return new AphidsAffliction(); }

            public void TreatWith(ITreatment treatment, PlantController plant)
            {
                if (treatment is HorticulturalOilTreatment or ImidaclopridTreatment or SpinosadTreatment
                    or InsecticideTreatment or Panacea)
                {
                    var infectReduction = treatment.InfectCureValue ?? 0;
                    var eggReduction = treatment.EggCureValue ?? 0;
                    plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                }
            }

            public void TickDay(PlantController plant)
            {
                if (!plant.PlantCard.Value.HasValue) return;
                var newVal = Mathf.Max(0, plant.PlantCard.Value.Value - 1);
                plant.PlantCard.Value = newVal;
                plant.UpdatePriceFlag(newVal);
            }
            public ICard GetCard() { return new AphidsCard(); }
        }
        
        public class SpiderMitesAffliction : IAffliction
        {
            public string Name => "Spider Mites";
            public string Description => "";
            public Color Color => Color.orange;
            public Shader Shader => null; //Shader.Find($"Shader Graphs/SpiderMites");
            public IAffliction Clone() { return new SpiderMitesAffliction(); }
            
            public void TreatWith(ITreatment treatment, PlantController plant)
            {
                if (treatment is HorticulturalOilTreatment or Panacea)
                {
                    var infectReduction = treatment.InfectCureValue ?? 0;
                    var eggReduction = treatment.EggCureValue ?? 0;
                    plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                }
            }

            public void TickDay(PlantController plant)
            {
                if (!plant.PlantCard.Value.HasValue) return;
                var newVal = Mathf.Max(0, plant.PlantCard.Value.Value - 1);
                plant.PlantCard.Value = newVal;
                plant.UpdatePriceFlag(newVal);
            }
            public ICard GetCard() { return new SpiderMitesCard(); }
        }
        
        public class FungusGnatsAffliction : IAffliction
        {
            public string Name => "Fungus Gnats";
            public string Description => "";
            public Color Color => Color.deepPink;
            public Shader Shader => null; //Shader.Find($"Shader Graphs/FungusGnats");

            public IAffliction Clone() { return new FungusGnatsAffliction(); }
            
            public void TreatWith(ITreatment treatment, PlantController plant)
            {
                if (treatment is ImidaclopridTreatment or SpinosadTreatment or Panacea)
                {
                    var infectReduction = treatment.InfectCureValue ?? 0;
                    var eggReduction = treatment.EggCureValue ?? 0;
                    plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                }
            }

            public void TickDay(PlantController plant)
            {
                if (!plant.PlantCard.Value.HasValue) return;
                var newVal = Mathf.Max(0, plant.PlantCard.Value.Value - 1);
                plant.PlantCard.Value = newVal;
                plant.UpdatePriceFlag(newVal);
            }
            public ICard GetCard() { return new FungusGnatsCard(); }
        }

        #endregion

        #region ITreatments

        public class HorticulturalOilTreatment : ITreatment
        {
            public string Name => "Horticultural Oil";
            public string Description => "Removes Aphids & Thrips";
            public int BeeValue => -1;
            private int _infectCureValue = StandardCureAmount;
            private int _eggCureValue = StandardCureAmount;
            private int _efficacy = DefaultEfficacy; //TODO
            public int? Efficacy
            {
                get => _efficacy;
                set => _efficacy = value ?? 0;
            }
            public int? InfectCureValue
            {
                get => _infectCureValue;
                set => _infectCureValue = value ?? 0;
            }

            public int? EggCureValue
            {
                get => _eggCureValue;
                set => _eggCureValue = value ?? 0;
            }
        }

        public class FungicideTreatment : ITreatment
        {
            public string Name => "Fungicide";
            public string Description => "Removes Mildew";
            public int BeeValue => -3;
            private int _infectCureValue = StandardCureAmount;
            private int _eggCureValue = StandardCureAmount;
            private int _efficacy = DefaultEfficacy; //TODO
            public int? Efficacy
            {
                get => _efficacy;
                set => _efficacy = value ?? 0;
            }
            public int? InfectCureValue
            {
                get => _infectCureValue;
                set => _infectCureValue = value ?? 0;
            }

            public int? EggCureValue
            {
                get => _eggCureValue;
                set => _eggCureValue = value ?? 0;
            }
        }

        public class InsecticideTreatment : ITreatment
        {
            public string Name => "Insecticide";
            public string Description => "Removes Insects";
            public int BeeValue => -4;
            private int _infectCureValue = 1;
            private int _eggCureValue;
            private int _efficacy = DefaultEfficacy; //TODO
            public int? Efficacy
            {
                get => _efficacy;
                set => _efficacy = value ?? 0;
            }
            public int? InfectCureValue
            {
                get => _infectCureValue;
                set => _infectCureValue = value ?? 0;
            }

            public int? EggCureValue
            {
                get => _eggCureValue;
                set => _eggCureValue = value ?? 0;
            }
        }

        public class SoapyWaterTreatment : ITreatment
        {
            public string Name => "SoapyWater";
            public string Description => "Removes MealyBugs";
            public int BeeValue => 0;
            private int _infectCureValue = StandardCureAmount;
            private int _eggCureValue = StandardCureAmount;
            private int _efficacy = DefaultEfficacy; //TODO
            public int? Efficacy
            {
                get => _efficacy;
                set => _efficacy = value ?? 0;
            }
            public int? InfectCureValue
            {
                get => _infectCureValue;
                set => _infectCureValue = value ?? 0;
            }

            public int? EggCureValue
            {
                get => _eggCureValue;
                set => _eggCureValue = value ?? 0;
            }
        }

        public class SpinosadTreatment : ITreatment
        {
            public string Name => "Spinosad";
            public string Description => "Effective against: Thrips, Mites, Gnats";
            public int BeeValue => -2; //TODO Get Bee Value
            private int _infectCureValue = StandardCureAmount;
            private int _eggCureValue = StandardCureAmount;
            private int _efficacy = DefaultEfficacy; //TODO
            public int? Efficacy
            {
                get => _efficacy;
                set => _efficacy = value ?? 0;
            }
            public int? InfectCureValue
            {
                get => _infectCureValue;
                set => _infectCureValue = value ?? 0;
            }

            public int? EggCureValue
            {
                get => _eggCureValue;
                set => _eggCureValue = value ?? 0;
            }
            
        }

        public class ImidaclopridTreatment : ITreatment
        {
            public string Name => "Imidacloprid";
            public string Description => "Insecticide, systemic, neonicotinoid, effective, broad-spectrum.";
            public int BeeValue => -5; //TODO Get Bee Value
            private int _infectCureValue = StandardCureAmount;
            private int _eggCureValue = StandardCureAmount;
            private int _efficacy = DefaultEfficacy; //TODO
            public int? Efficacy
            {
                get => _efficacy;
                set => _efficacy = value ?? 0;
            }
            public int? InfectCureValue
            {
                get => _infectCureValue;
                set => _infectCureValue = value ?? 0;
            }

            public int? EggCureValue
            {
                get => _eggCureValue;
                set => _eggCureValue = value ?? 0;
            }
        }

        public class Panacea : ITreatment
        {
            public string Name => "Panacea";
            public string Description => "Cures All Afflictions";
            public int BeeValue => 0;
            private int _infectCureValue = PanaceaCureAmount;
            private int _eggCureValue = PanaceaCureAmount;
            private int _efficacy = DefaultEfficacy; //TODO
            public int? Efficacy
            {
                get => _efficacy;
                set => _efficacy = value ?? 0;
            }
            public int? InfectCureValue
            {
                get => _infectCureValue;
                set => _infectCureValue = value ?? 0;
            }

            public int? EggCureValue
            {
                get => _eggCureValue;
                set => _eggCureValue = value ?? 0;
            }
        }

        #endregion
    }
}