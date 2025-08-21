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
        public List<ISticker> Stickers => new();
        public LocationEffectType EffectType => null;

        public ICard Clone()
        {
            return new FertilizerBasic();
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

    public class ColeusCard : ICard
    {
        public PlantType Type => PlantType.Coleus;
        public string Name => "Coleus";
        private int _value = 5;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;

        public List<ISticker> Stickers => new();
        public ICard Clone() { return new ColeusCard(); }
        public void ModifyValue(int delta) => Value = (Value ?? 0) + delta;
    }

    public class ChrysanthemumCard : ICard
    {
        public PlantType Type => PlantType.Chrysanthemum;
        public string Name => "Chrysanthemum";
        private int _value = 8;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public List<ISticker> Stickers => new();

        public ICard Clone() { return new ChrysanthemumCard(); }
        public void ModifyValue(int delta) => Value = (Value ?? 0) + delta;
    }

    public class PepperCard : ICard
    {
        public PlantType Type => PlantType.Pepper;
        public string Name => "Pepper";
        private int _value = 4;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public List<ISticker> Stickers => new();

        public ICard Clone() { return new PepperCard(); }
        public void ModifyValue(int delta) => Value = (Value ?? 0) + delta;
    }

    public class CucumberCard : ICard
    {
        public PlantType Type => PlantType.Cucumber;
        public string Name => "Cucumber";
        private int _value = 3;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public List<ISticker> Stickers => new();

        public ICard Clone() { return new CucumberCard(); }
        public void ModifyValue(int delta) => Value = (Value ?? 0) + delta;
    }

    #endregion

    #region Disease Cards

    public class AphidsCard : ICard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.AphidsAffliction();
        public string Name => "Aphids";
        public int? Value => -2;
        public List<ISticker> Stickers => new();

        public ICard Clone() { return new AphidsCard(); }
    }

    public class MealyBugsCard : ICard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.MealyBugsAffliction();
        public string Name => "Mealy Bugs";
        public int? Value => -4;
        public List<ISticker> Stickers => new();

        public ICard Clone() { return new MealyBugsCard(); }
    }

    public class ThripsCard : ICard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.ThripsAffliction();
        public string Name => "Thrips";
        public int? Value => -5;
        public List<ISticker> Stickers => new();

        public ICard Clone() { return new ThripsCard(); }
    }

    public class MildewCard : ICard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.MildewAffliction();
        public string Name => "Mildew";
        public int? Value => -4;
        public List<ISticker> Stickers => new();
        public ICard Clone() { return new MildewCard(); }
    }

    public class SpiderMitesCard : ICard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.SpiderMitesAffliction();
        public string Name => "Spider Mites";
        public int? Value => -3;
        public List<ISticker> Stickers => new();
        public ICard Clone() { return new SpiderMitesCard(); }
    }

    public class FungusGnatsCard : ICard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.FungusGnatsAffliction();
        public string Name => "Fungus Gnats";
        public int? Value => -2;
        public List<ISticker> Stickers => new();
        public ICard Clone() { return new FungusGnatsCard(); }
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
        public List<ISticker> Stickers => new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/NeemOil");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { return new HorticulturalOilBasic(); }
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
        public List<ISticker> Stickers => new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/SyntheticInsecticide");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { return new InsecticideBasic(); }
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

        public List<ISticker> Stickers => new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/Fungicide");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { return new FungicideBasic(); }
    }

    public class SoapyWaterBasic : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.SoapyWaterTreatment();
        public string Name => "Soapy Water Basic";
        public List<ISticker> Stickers => new();
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
        public ICard Clone() { return new SoapyWaterBasic(); }
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
        public List<ISticker> Stickers => new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/Spinosad");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { return new SpinosadTreatment(); }
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
        public List<ISticker> Stickers => new();
        
        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/Imidacloprid");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { return new ImidaclopridTreatment(); }
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
        public List<ISticker> Stickers => new();

        public string Description
        {
            set => _description = value;
            get => _description ?? Treatment.Description;
        }

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/Panacea");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone() { return new Panacea(); }
    }

    #endregion
}
