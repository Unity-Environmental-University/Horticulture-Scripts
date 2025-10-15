using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Analytics;
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

        private static int ResolveTreatmentEfficacy(IAffliction affliction, ITreatment treatment)
        {
            var handler = CardGameMaster.Instance?.treatmentEfficacyHandler;
            var efficacy = handler
                ? handler.GetRelationalEfficacy(affliction, treatment)
                : treatment.Efficacy ?? DefaultEfficacy;
            return Mathf.Clamp(efficacy, 0, 100);
        }

        private static bool TreatmentAttemptSucceeds(IAffliction affliction, ITreatment treatment)
        {
            var chance = ResolveTreatmentEfficacy(affliction, treatment);
            return Random.Range(0, 100) < chance;
        }

        public interface IAffliction
        {
            string Name { get; }
            string Description { get; }
            public Color Color { get; }
            [CanBeNull] public Shader Shader { get; }
            public List<ITreatment> AcceptableTreatments { get; }
            public bool TreatWith(ITreatment treatment, PlantController plant);
            public void TickDay(PlantController plant);

            [CanBeNull]
            public ICard GetCard() { return null; }

            public IAffliction Clone();
            public bool CanBeTreatedBy(ITreatment treatment) { return false; }
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
                    var success = item.TreatWith(this, plant);
                    TreatmentAnalytics.RecordTreatment(plant.name, item.Name, Name, success);

                    if (CardGameMaster.Instance != null && CardGameMaster.Instance.debuggingCardClass)
                        Debug.Log($"Applied treatment to affliction: {item.Name}");
                }
            }
        }

        #region IAfflictions

        public class ThripsAffliction : IAffliction
        {
            // ReSharper disable twice NotAccessedField.Local
            private bool _hasAdults = true;
            private bool _hasLarvae = true;

            private static readonly List<ITreatment> Treatments = new()
            {
                new Panacea(),
                new InsecticideTreatment(),
                new HorticulturalOilTreatment()
            };

            public string Name => "Thrips";
            public string Description => "";
            public Color Color => Color.black;
            public Shader Shader => Shader.Find($"Shader Graphs/Thrips");

            public List<ITreatment> AcceptableTreatments => Treatments;

            public IAffliction Clone() { return new ThripsAffliction(); }

            public bool CanBeTreatedBy(ITreatment treatment)
            {
                return Treatments.Any(t => t.GetType() == treatment.GetType());
            }

            public bool TreatWith(ITreatment treatment, PlantController plant)
            {
                if (!CanBeTreatedBy(treatment))
                {
                    return false;
                }

                var affectsAdults = treatment is InsecticideTreatment or Panacea;
                var affectsLarvae = treatment is HorticulturalOilTreatment or Panacea;

                // Get actual current values from the plant (not internal flags)
                var currentInfect = plant.GetInfectFrom(this);
                var currentEggs = plant.GetEggsFrom(this);

                var infectReduction = affectsAdults && currentInfect > 0
                    ? treatment.InfectCureValue ?? 0
                    : 0;
                var eggReduction = affectsLarvae && currentEggs > 0
                    ? treatment.EggCureValue ?? 0
                    : 0;

                if (infectReduction <= 0 && eggReduction <= 0)
                {
                    return false;
                }

                if (!TreatmentAttemptSucceeds(this, treatment))
                {
                    return false;
                }

                if (infectReduction > 0)
                {
                    _hasAdults = false;
                }

                if (eggReduction > 0)
                {
                    _hasLarvae = false;
                }

                plant.ReduceAfflictionValues(this, infectReduction, eggReduction);

                // Update internal flags based on remaining values after treatment
                var remainingInfect = plant.GetInfectFrom(this);
                var remainingEggs = plant.GetEggsFrom(this);
                _hasAdults = remainingInfect > 0;
                _hasLarvae = remainingEggs > 0;
                return true;
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
            private static readonly List<ITreatment> Treatments = new()
            {
                new SoapyWaterTreatment(),
                new InsecticideTreatment(),
                new ImidaclopridTreatment(),
                new Panacea()
            };

            public string Name => "MealyBugs";
            public string Description => "";
            public Color Color => Color.red;
            public Shader Shader => Shader.Find($"Shader Graphs/MealyBugs");

            public List<ITreatment> AcceptableTreatments => Treatments;

            public IAffliction Clone() { return new MealyBugsAffliction(); }

            public bool CanBeTreatedBy(ITreatment treatment)
            {
                return Treatments.Any(t => t.GetType() == treatment.GetType());
            }

            public bool TreatWith(ITreatment treatment, PlantController plant)
            {
                if (!CanBeTreatedBy(treatment)) return false;
                if (!TreatmentAttemptSucceeds(this, treatment))
                {
                    return false;
                }

                var infectReduction = treatment.InfectCureValue ?? 0;
                var eggReduction = treatment.EggCureValue ?? 0;
                plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                return true;
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
            private static readonly List<ITreatment> Treatments = new()
            {
                new FungicideTreatment(),
                new Panacea()
            };

            public string Name => "Mildew";
            public string Description => "";
            public Color Color => Color.white;
            public Shader Shader => Shader.Find($"Shader Graphs/Mold");

            public List<ITreatment> AcceptableTreatments => Treatments;

            public IAffliction Clone() { return new MildewAffliction(); }

            public bool CanBeTreatedBy(ITreatment treatment)
            {
                return Treatments.Any(t => t.GetType() == treatment.GetType());
            }

            public bool TreatWith(ITreatment treatment, PlantController plant)
            {
                if (!CanBeTreatedBy(treatment)) return false;
                if (!TreatmentAttemptSucceeds(this, treatment))
                {
                    return false;
                }

                var infectReduction = treatment.InfectCureValue ?? 0;
                var eggReduction = treatment.EggCureValue ?? 0;
                plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                return true;
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
            private static readonly List<ITreatment> Treatments = new()
            {
                new HorticulturalOilTreatment(),
                new ImidaclopridTreatment(),
                new SpinosadTreatment(),
                new InsecticideTreatment(),
                new Panacea()
            };

            public string Name => "Aphids";
            public string Description => "";
            public Color Color => Color.cyan;
            public Shader Shader => Shader.Find($"Shader Graphs/Aphids");

            public List<ITreatment> AcceptableTreatments => Treatments;

            public IAffliction Clone() { return new AphidsAffliction(); }

            public bool CanBeTreatedBy(ITreatment treatment)
            {
                return Treatments.Any(t => t.GetType() == treatment.GetType());
            }

            public bool TreatWith(ITreatment treatment, PlantController plant)
            {
                if (!CanBeTreatedBy(treatment)) return false;
                if (!TreatmentAttemptSucceeds(this, treatment))
                {
                    return false;
                }

                var infectReduction = treatment.InfectCureValue ?? 0;
                var eggReduction = treatment.EggCureValue ?? 0;
                plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                return true;
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
            private static readonly List<ITreatment> Treatments = new()
            {
                new HorticulturalOilTreatment(),
                new Panacea()
            };

            public string Name => "Spider Mites";
            public string Description => "";
            public Color Color => Color.orange;
            public Shader Shader => Shader.Find($"Shader Graphs/SpiderMites");

            public List<ITreatment> AcceptableTreatments => Treatments;

            public IAffliction Clone() { return new SpiderMitesAffliction(); }

            public bool CanBeTreatedBy(ITreatment treatment)
            {
                return Treatments.Any(t => t.GetType() == treatment.GetType());
            }
            
            public bool TreatWith(ITreatment treatment, PlantController plant)
            {
                if (!CanBeTreatedBy(treatment)) return false;
                if (!TreatmentAttemptSucceeds(this, treatment))
                {
                    return false;
                }

                var infectReduction = treatment.InfectCureValue ?? 0;
                var eggReduction = treatment.EggCureValue ?? 0;
                plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                return true;
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
            private static readonly List<ITreatment> Treatments = new()
            {
                new ImidaclopridTreatment(),
                new SpinosadTreatment(),
                new Panacea()
            };

            public string Name => "Fungus Gnats";
            public string Description => "";
            public Color Color => Color.deepPink;
            public Shader Shader => Shader.Find($"Shader Graphs/FungusGnats");

            public List<ITreatment> AcceptableTreatments => Treatments;

            public IAffliction Clone() { return new FungusGnatsAffliction(); }

            public bool CanBeTreatedBy(ITreatment treatment)
            {
                return Treatments.Any(t => t.GetType() == treatment.GetType());
            }
            
            public bool TreatWith(ITreatment treatment, PlantController plant)
            {
                if (!CanBeTreatedBy(treatment)) return false;
                if (!TreatmentAttemptSucceeds(this, treatment))
                {
                    return false;
                }

                var infectReduction = treatment.InfectCureValue ?? 0;
                var eggReduction = treatment.EggCureValue ?? 0;
                plant.ReduceAfflictionValues(this, infectReduction, eggReduction);
                return true;
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