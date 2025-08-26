using System;
using System.Collections.Generic;
using _project.Scripts.Card_Core;
using _project.Scripts.Core;
using _project.Scripts.Stickers;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace _project.Scripts.Classes
{
    public interface ICard
    {
        string Name { get; }
        string Description => null;
        int? Value
        {
            get => null;
            set => throw new NotImplementedException();
        }

        PlantAfflictions.IAffliction Affliction => null;
        PlantAfflictions.ITreatment Treatment => null;
        GameObject Prefab => null;
        Material Material => null;
        List<ISticker> Stickers { get; }
        ICard Clone();

        void Selected() { }

        void ApplySticker(ISticker sticker) { Stickers.Add(sticker); }
        void ModifyValue(int delta) { }
    }

    public interface IPlantCard : ICard
    {
        InfectLevel Infect { get; }
        int EggLevel { get; set; }
    }

    public interface IAfflictionCard : ICard
    {
        int BaseInfectLevel { get; set; }
        int BaseEggLevel { get; set; }
    }


    #region Decks

    public class CardHand : List<ICard>
    {
        public CardHand(string name, List<ICard> deck, List<ICard> prototypeDeck)
        {
            Name = name;
            Deck = deck;
            PrototypeDeck = prototypeDeck;
        }

        public string Name { get; }
        public List<ICard> Deck { get; }
        private List<ICard> PrototypeDeck { get; }

        public void DrawCards(int number)
        {
            if (Deck.Count == 0) return;
            number = Mathf.Min(number, Deck.Count);
            for (var i = 0; i < number; i++)
            {
                var drawnCard = Deck[0];
                Add(drawnCard);
                Deck.RemoveAt(0);
            }
        }

        public void DeckRandomDraw()
        {
            Deck.Clear();
            foreach (var card in PrototypeDeck)
            {
                // Randomly duplicate each prototype card between 1 and 4 times.
                const int minDuplicates = 1;
                const int maxDuplicates = 5; // Exclusive upper bound (returns 1 to 4 copies)
                var duplicates = Random.Range(minDuplicates, maxDuplicates);
                for (var i = 0; i < duplicates; i++) Deck.Add(card.Clone());
            }
        }
    }

    #endregion

    #region Plant Location Cards

    public interface ILocationCard : ICard
    {
        int EffectDuration { get; }
        bool IsPermanent { get; }
        LocationEffectType EffectType { get; }

        void ApplyLocationEffect(PlantController plant);
        void RemoveLocationEffect(PlantController plant);
    }

    public abstract class LocationEffectType { }
    
    public class FertilizerBasic : ILocationCard
    {
        public string Name => "FertilizerBasic";
        public string Description => "Does FertilizerBasic Things";

        private int _value = -1;

        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public int EffectDuration => IsPermanent ? 999 : 3;
        public bool IsPermanent => false;
        public GameObject Prefab => CardGameMaster.Instance.locationCardPrefab;
        public Material Material => Resources.Load<Material>("Materials/Cards/NeemOil");
        public List<ISticker> Stickers { get; } = new();

        public LocationEffectType EffectType => null;

        public ICard Clone()
        {
            var clone = new FertilizerBasic { Value = this.Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }

        public void Selected()
        {
            if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name);
        }

        public void ModifyValue(int delta)
        {
            _value += delta;
        }

        public void ApplyLocationEffect(PlantController plant)
        {
            throw new NotImplementedException();
            // do make the plant more valuable 20% over 3 turns
        }

        public void RemoveLocationEffect(PlantController plant)
        {
            throw new NotImplementedException();
            // will remove/destroy itself
        }
    }

    #endregion

    #region PlantCards
    
    public class ColeusCard : IPlantCard
    {
        public PlantType Type => PlantType.Coleus;
        public string Name => "Coleus";
        private int _value = 5;
        public InfectLevel Infect { get; } = new();

        public int EggLevel
        {
            get => Infect.EggTotal;
            set => Infect.SetEggs("Manual", Mathf.Max(0, value));
        }

        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new ColeusCard { EggLevel = this.EggLevel, Value = this.Value };
            foreach (var kv in Infect.All)
            {
                clone.Infect.SetInfect(kv.Key, kv.Value.infect);
                clone.Infect.SetEggs(kv.Key, kv.Value.eggs);
            }
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
        public void ModifyValue(int delta) => Value = (Value ?? 0) + delta;
    }

    public class ChrysanthemumCard : IPlantCard
    {
        public PlantType Type => PlantType.Chrysanthemum;
        public string Name => "Chrysanthemum";
        private int _value = 8;

        public InfectLevel Infect { get; } = new();

        public int EggLevel 
        { 
            get => Infect.EggTotal; 
            set => Infect.SetEggs("Manual", Mathf.Max(0, value));
        }

        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public List<ISticker> Stickers { get; } = new();

        public ICard Clone() {
            var clone = new ChrysanthemumCard { EggLevel = this.EggLevel, Value = this.Value };
            foreach (var kv in Infect.All) {
                clone.Infect.SetInfect(kv.Key, kv.Value.infect);
                clone.Infect.SetEggs(kv.Key, kv.Value.eggs);
            }
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
        public void ModifyValue(int delta) => Value = (Value ?? 0) + delta;
    }

    public class PepperCard : IPlantCard
    {
        public PlantType Type => PlantType.Pepper;
        public string Name => "Pepper";
        private int _value = 4;

        public InfectLevel Infect { get; } = new();

        public int EggLevel 
        { 
            get => Infect.EggTotal; 
            set => Infect.SetEggs("Manual", Mathf.Max(0, value));
        }

        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public List<ISticker> Stickers { get; } = new();

        public ICard Clone() {
            var clone = new PepperCard { EggLevel = this.EggLevel, Value = this.Value };
            foreach (var kv in Infect.All) {
                clone.Infect.SetInfect(kv.Key, kv.Value.infect);
                clone.Infect.SetEggs(kv.Key, kv.Value.eggs);
            }
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
        public void ModifyValue(int delta) => Value = (Value ?? 0) + delta;
    }

    public class CucumberCard : IPlantCard
    {
        public PlantType Type => PlantType.Cucumber;
        public string Name => "Cucumber";
        private int _value = 3;

        public InfectLevel Infect { get; } = new();

        public int EggLevel 
        { 
            get => Infect.EggTotal; 
            set => Infect.SetEggs("Manual", Mathf.Max(0, value));
        }

        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public List<ISticker> Stickers { get; } = new();

        public ICard Clone() {
            var clone = new CucumberCard { EggLevel = this.EggLevel, Value = this.Value };
            foreach (var kv in Infect.All) {
                clone.Infect.SetInfect(kv.Key, kv.Value.infect);
                clone.Infect.SetEggs(kv.Key, kv.Value.eggs);
            }
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
        public void ModifyValue(int delta) => Value = (Value ?? 0) + delta;
    }

    #endregion

    #region Affliction Cards

    public class AphidsCard : IAfflictionCard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.AphidsAffliction();
        public string Name => "Aphids";
        public int? Value => -2;

        private int _baseInfectLevel = 1;
        private int _baseEggLevel;

        public int BaseInfectLevel
        {
            get => _baseInfectLevel;
            set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new AphidsCard();
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class MealyBugsCard : IAfflictionCard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.MealyBugsAffliction();
        public string Name => "Mealy Bugs";
        public int? Value => -4;
        private int _baseInfectLevel = 1;
        private int _baseEggLevel;

        public int BaseInfectLevel
        {
            get => _baseInfectLevel;
            set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new MealyBugsCard();
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class ThripsCard : IAfflictionCard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.ThripsAffliction();
        public string Name => "Thrips";
        public int? Value => -5;
        private int _baseInfectLevel = 1;
        private int _baseEggLevel = 1;

        public int BaseInfectLevel
        {
            get => _baseInfectLevel;
            set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new ThripsCard();
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class MildewCard : IAfflictionCard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.MildewAffliction();
        public string Name => "Mildew";
        public int? Value => -4;
        private int _baseInfectLevel = 1;
        private int _baseEggLevel;

        public int BaseInfectLevel
        {
            get => _baseInfectLevel;
            set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new MildewCard();
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class SpiderMitesCard : IAfflictionCard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.SpiderMitesAffliction();
        public string Name => "Spider Mites";
        public int? Value => -3;
        private int _baseInfectLevel = 1;
        private int _baseEggLevel;

        public int BaseInfectLevel
        {
            get => _baseInfectLevel;
            set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new SpiderMitesCard();
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class FungusGnatsCard : IAfflictionCard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.FungusGnatsAffliction();
        public string Name => "Fungus Gnats";
        public int? Value => -2;
        private int _baseInfectLevel = 1;
        private int _baseEggLevel;

        public int BaseInfectLevel
        {
            get => _baseInfectLevel;
            set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new FungusGnatsCard();
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    #endregion

    #region Action Cards

    public class HorticulturalOilBasic : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.HorticulturalOilTreatment();
        public string Name => "Horticultural Oil Basic";
        private int _value = -1;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public List<ISticker> Stickers { get; } = new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/NeemOil");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { 
            var clone = new HorticulturalOilBasic { Value = this.Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class InsecticideBasic : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.InsecticideTreatment();
        public string Name => "Synthetic Insecticide Basic";
        private int _value = -3;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public List<ISticker> Stickers { get; } = new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/SyntheticInsecticide");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { 
            var clone = new InsecticideBasic { Value = this.Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class FungicideBasic : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.FungicideTreatment();
        public string Name => "Fungicide Basic";
        private int _value = -2;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public List<ISticker> Stickers { get; } = new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/Fungicide");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { 
            var clone = new FungicideBasic { Value = this.Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class SoapyWaterBasic : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.SoapyWaterTreatment();
        public string Name => "Soapy Water Basic";
        public List<ISticker> Stickers { get; } = new();

        private int _value = -1;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/SoapyWater");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { 
            var clone = new SoapyWaterBasic { Value = this.Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }
    
    public class SpinosadTreatment : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.SpinosadTreatment();
        public string Name => "Spinosad";
        private int _value = -4;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public List<ISticker> Stickers { get; } = new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/Spinosad");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { 
            var clone = new SpinosadTreatment { Value = this.Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class ImidaclopridTreatment : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.ImidaclopridTreatment();
        public string Name => "Imidacloprid";
        private int _value = -2;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public List<ISticker> Stickers { get; } = new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/Imidacloprid");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { 
            var clone = new ImidaclopridTreatment { Value = this.Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class Panacea : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.Panacea();
        public string Name => "Panacea";
        private int _value = -5;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public List<ISticker> Stickers { get; } = new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/Panacea");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { 
            var clone = new Panacea { Value = this.Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    #endregion
}
