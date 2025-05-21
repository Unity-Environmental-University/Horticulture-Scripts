using System.Collections.Generic;
using _project.Scripts.Core;
using JetBrains.Annotations;
using UnityEngine;

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
            public void TickDay();

            [CanBeNull]
            public ICard GetCard() { return null; }
        }

        public interface ITreatment
        {
            string Name { get; }
            string Description { get; }
            int BeeValue { get; }

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
            public Shader Shader => null;

            public void TreatWith(ITreatment treatment, PlantController plant)
            {
                switch (treatment.Name)
                {
                    case "Insecticide":
                        _hasAdults = false;
                        //Debug.LogError("HadAdults: " + _hasAdults + "HasLarvae " + _hasLarvae);
                        break;
                    case "NeemOil":
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

            public void TickDay()
            {
                if (_hasAdults) _hasLarvae = true;
                else if (_hasLarvae) _hasAdults = true;
            }

            public ICard GetCard() { return new ThripsCard(); }
        }

        public class MealyBugsAffliction : IAffliction
        {
            public string Name => "MealyBugs";
            public string Description => "";
            public Color Color => Color.red;
            public Shader Shader => null;

            public void TreatWith(ITreatment treatment, PlantController plant)
            {
                if (treatment.Name is "SoapyWater" or "Panacea") plant.RemoveAffliction(this);
            }

            public void TickDay() { }
            public ICard GetCard() { return new MealyBugsCard(); }
        }

        public class MildewAffliction : IAffliction
        {
            public string Name => "Mildew";
            public string Description => "";
            public Color Color => Color.white;
            public Shader Shader => null;

            public void TreatWith(ITreatment treatment, PlantController plant)
            {
                if (treatment.Name is "Fungicide" or "Panacea") plant.RemoveAffliction(this);
            }

            public void TickDay() { }
            public ICard GetCard() { return new MildewCard(); }
        }

        public class AphidsAffliction : IAffliction
        {
            public string Name => "Aphids";
            public string Description => "";
            public Color Color => Color.cyan;
            public Shader Shader => null;

            public void TreatWith(ITreatment treatment, PlantController plant)
            {
                if (treatment.Name is "NeemOil" or "Panacea") plant.RemoveAffliction(this);
            }

            public void TickDay() { }
            public ICard GetCard() { return new AphidsCard(); }
        }

        #endregion

        #region ITreatments

        public class NeemOilTreatment : ITreatment
        {
            public string Name => "NeemOil";
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

        public class Panacea : ITreatment
        {
            public string Name => "Panacea";
            public string Description => "Cures All Afflictions";
            public int BeeValue => 0;
        }

        #endregion
    }
}