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
                    if (CardGameMaster.Instance.debuggingCardClass) Debug.Log($"Applied treatment to affliction: {item.Name}");
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
                switch (treatment.Name)
                {
                    case "Insecticide":
                        _hasAdults = false;
                        //Debug.LogError("HadAdults: " + _hasAdults + "HasLarvae " + _hasLarvae);
                        break;
                    case "Horticultural Oil":
                        _hasLarvae = false;
                        //Debug.LogError("HadAdults: " + _hasAdults + "HasLarvae " + _hasLarvae);
                        break;
                    case "Panacea":
                        _hasAdults = false;
                        _hasLarvae = false;
                        //Debug.LogError("HadAdults: " + _hasAdults + "HasLarvae " + _hasLarvae);
                        break;
                }

                if (!_hasAdults && !_hasLarvae) plant.RemoveAffliction(this);
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
                    plant.RemoveAffliction(this);
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
                if (treatment is FungicideTreatment or Panacea) plant.RemoveAffliction(this);
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
                    or InsecticideTreatment or Panacea) plant.RemoveAffliction(this);
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
                if (treatment is HorticulturalOilTreatment or Panacea) plant.RemoveAffliction(this);
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
                if (treatment is ImidaclopridTreatment or SpinosadTreatment or Panacea) plant.RemoveAffliction(this);
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
        }

        public class FungicideTreatment : ITreatment
        {
            public string Name => "Fungicide";
            public string Description => "Removes Mildew";
            public int BeeValue => -3;
        }

        public class InsecticideTreatment : ITreatment
        {
            public string Name => "Insecticide";
            public string Description => "Removes Insects";
            public int BeeValue => -4;
        }

        public class SoapyWaterTreatment : ITreatment
        {
            public string Name => "SoapyWater";
            public string Description => "Removes MealyBugs";
            public int BeeValue => 0;
        }

        public class SpinosadTreatment : ITreatment
        {
            public string Name => "Spinosad";
            public string Description => "Effective against: Thrips, Mites, Gnats";
            public int BeeValue => -2;
        }

        public class ImidaclopridTreatment : ITreatment
        {
            public string Name => "Imidacloprid";
            public string Description => "Removes Mildew"; // TODO Get exact list
            public int BeeValue => -5; //TODO Get Bee Value
        }

        public class Panacea : ITreatment
        {
            public string Name => "Panacea";
            public string Description => "Cures All Afflictions";
            public int BeeValue => 0;
        }

        #endregion
    }
}