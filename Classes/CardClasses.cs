using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Core;
using _project.Scripts.Stickers;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedMember.Global

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

    public enum PlantCardCategory
    {
        Fruiting,
        Decorative,
        Other
    }

    public interface IPlantCard : ICard
    {
        /// <summary>
        /// The base value used for diminishing returns calculations by location cards.
        /// Automatically set when the first location card effect is applied.
        /// </summary>
        /// <remarks>
        /// <para>This property supports location cards like UreaBasic that provide cumulative
        /// diminishing returns. The BaseValue is typically set to the plant's value at the time
        /// of the first application and remains constant for subsequent boost calculations.</para>
        /// <para><b>Important:</b> This should only be modified by ILocationCard implementations.
        /// Direct modification by game code will break diminishing returns calculations and
        /// cause incorrect pricing.</para>
        /// <para>This value persists across save/load operations via GameStateManager.</para>
        /// </remarks>
        int BaseValue { get; set; }
        InfectLevel Infect { get; }
        int EggLevel { get; set; }
        PlantCardCategory Category { get; }
    }

    public interface IAfflictionCard : ICard
    {
        int BaseInfectLevel { get; }
        int BaseEggLevel { get; }
    }

    public interface IFieldSpell : ICard
    {
        bool AffectsAllPlants { get; set; }
        bool ShowsGhosts { get; set; }
        bool TillDeath { get; set; }
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
                // Generate 1-4 random copies of each prototype card
                const int minDuplicates = 1;
                const int maxDuplicates = 5;
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
        void ApplyTurnEffect(PlantController plant);
    }

    public abstract class LocationEffectType { }

    /// <summary>
    /// Location card that enriches soil with nitrogen-rich urea, providing cumulative
    /// diminishing returns price boosts to plants.
    /// </summary>
    /// <remarks>
    /// <para>The first application doubles the plant's value. Subsequent applications
    /// add progressively smaller boosts (50%, 33%, 25%, etc. of the original value).</para>
    /// <para>This implements a diminishing returns model to balance gameplay and prevent
    /// exponential value growth from repeated applications.</para>
    /// </remarks>
    public class UreaBasic : ILocationCard
    {
        public string Name => "UreaBasic";
        public string Description => "Doubles the value of the plant by enriching the soil with nitrogen-rich urea.";
        private int _value = -1;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }
        public int EffectDuration => IsPermanent ? 999 : 3;
        public bool IsPermanent => false;
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/Urea");
        public List<ISticker> Stickers { get; } = new();
        public LocationEffectType EffectType => null;
        private PlantController _lastEffectedPlant;

        public ICard Clone()
        {
            var clone = new UreaBasic { Value = Value };
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

        /// <summary>
        /// Applies a cumulative diminishing returns price boost to the specified plant.
        /// </summary>
        /// <param name="plant">The plant to apply the Urea effect to. No effect if null or has no PlantCard.</param>
        /// <remarks>
        /// <para><b>First Application:</b> Doubles the plant's current value and stores it as BaseValue.</para>
        /// <para><b>Subsequent Applications:</b> Adds a diminishing boost calculated as
        /// <c>BaseValue × (1 / (applicationsCount + 1))</c>, rounded to the nearest integer.</para>
        /// <para><b>Value Cap:</b> Final value is capped at <c>BaseValue²</c> to prevent unbounded growth.</para>
        /// <para>Application count is tracked in <c>plant.uLocationCards</c> and persists across save/load.</para>
        /// </remarks>
        /// <example>
        /// For a plant with initial value 10:
        /// <code>
        /// urea.ApplyLocationEffect(plant);  // 10 → 20 (BaseValue = 10, 100% boost)
        /// urea.ApplyLocationEffect(plant);  // 20 → 25 (adds 5, 50% of BaseValue)
        /// urea.ApplyLocationEffect(plant);  // 25 → 28 (adds 3, 33% of BaseValue)
        /// urea.ApplyLocationEffect(plant);  // 28 → 30 (adds 2, 25% of BaseValue)
        /// </code>
        /// </example>
        public void ApplyLocationEffect(PlantController plant)
        {
            if (plant?.PlantCard?.Value == null) return;
            if (plant.PlantCard is not IPlantCard plantCard) return;

            _lastEffectedPlant = plant;
            if (plant.buffFX) plant.buffFX.Play();

            var currentValue = plant.PlantCard.Value.Value;
            var timesUsed = plant.uLocationCards.Count(lCard => lCard == Name);

            int newValue;
            if (timesUsed == 0)
            {
                // First use: establish BaseValue and double the current value
                plantCard.BaseValue = currentValue;
                newValue = currentValue * 2;
            }
            else
            {
                // Subsequent uses: add diminishing boost (1/(n+1) of BaseValue)
                var multiplier = 1.0f / (timesUsed + 1);
                var boost = Mathf.RoundToInt(plantCard.BaseValue * multiplier);
                newValue = currentValue + boost;
            }

            plant.uLocationCards.Add(Name);

            // Cap at BaseValue squared to prevent unbounded growth
            var maxPlantValue = plantCard.BaseValue * plantCard.BaseValue;
            plant.PlantCard.Value = Mathf.Min(newValue, maxPlantValue);
            plant.UpdatePriceFlag(plant.PlantCard.Value.Value);
        }

        public void RemoveLocationEffect(PlantController plant)
        {
            if (plant?.buffFX) plant.buffFX.Stop();
        }

        public void ApplyTurnEffect(PlantController plant)
        {
            if (plant?.PlantCard?.Value == null) return;
            if (plant != _lastEffectedPlant) ApplyLocationEffect(plant);
            if (plant.buffFX.isStopped) plant.buffFX.Play();

            foreach (var damage in plant.CurrentAfflictions.Select(affliction => affliction.GetCard()?.Value ?? 0))
                plant.PlantCard.Value = Mathf.Max(0, plant.PlantCard.Value.Value + damage + 1);

            plant.UpdatePriceFlag(plant.PlantCard.Value.Value);
        }
    }

    public class IsolateBasic : ILocationCard
    {
        public string Name => "IsolateBasic";

        public string Description =>
            "Isolates Applied Plant. Preventing Spread of Afflictions To and From the Applied Plant";

        private int _value = -5;

        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public int EffectDuration => IsPermanent ? 999 : 3;
        public bool IsPermanent => false;
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>("Materials/Cards/Isolate");
        public List<ISticker> Stickers { get; } = new();
        public LocationEffectType EffectType => null;

        public ICard Clone()
        {
            var clone = new IsolateBasic { Value = Value };
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
            plant.canSpreadAfflictions = false;
            plant.canReceiveAfflictions = false;
        }

        public void RemoveLocationEffect(PlantController plant)
        {
            plant.canSpreadAfflictions = true;
            plant.canReceiveAfflictions = true;
        }

        public void ApplyTurnEffect(PlantController plant)
        {
        }
    }

    #endregion

    #region PlantCards

    public class ColeusCard : IPlantCard
    {
        private const int InitialValue = 5;
        public PlantType Type => PlantType.Coleus;
        public string Name => "Coleus";
        private int _value = InitialValue;
        public int BaseValue { get; set; } = InitialValue;
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
        public PlantCardCategory Category => PlantCardCategory.Decorative;
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new ColeusCard { EggLevel = EggLevel, Value = Value, BaseValue = BaseValue };
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
        private const int InitialValue = 8;
        public PlantType Type => PlantType.Chrysanthemum;
        public string Name => "Chrysanthemum";
        private int _value = InitialValue;
        public int BaseValue { get; set; } = InitialValue;

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

        public PlantCardCategory Category => PlantCardCategory.Decorative;

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new ChrysanthemumCard { EggLevel = EggLevel, Value = Value, BaseValue = BaseValue };
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

    public class PepperCard : IPlantCard
    {
        private const int InitialValue = 4;
        public PlantType Type => PlantType.Pepper;
        public string Name => "Pepper";
        private int _value = InitialValue;
        public int BaseValue { get; set; } = InitialValue;

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
        public PlantCardCategory Category => PlantCardCategory.Fruiting;

        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new PepperCard { EggLevel = EggLevel, Value = Value, BaseValue = BaseValue };
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

    public class CucumberCard : IPlantCard
    {
        private const int InitialValue = 3;
        public PlantType Type => PlantType.Cucumber;
        public string Name => "Cucumber";
        private int _value = InitialValue;
        public int BaseValue { get; set; } = InitialValue;

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
        public PlantCardCategory Category => PlantCardCategory.Fruiting;
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new CucumberCard { EggLevel = EggLevel, Value = Value, BaseValue = BaseValue };
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
            private set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            private set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new AphidsCard
            {
                BaseInfectLevel = BaseInfectLevel,
                BaseEggLevel = BaseEggLevel
            };
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
            private set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            private set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new MealyBugsCard
            {
                BaseInfectLevel = BaseInfectLevel,
                BaseEggLevel = BaseEggLevel
            };
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
            private set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            private set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new ThripsCard
            {
                BaseInfectLevel = BaseInfectLevel,
                BaseEggLevel = BaseEggLevel
            };
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
            private set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            private set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new MildewCard
            {
                BaseInfectLevel = BaseInfectLevel,
                BaseEggLevel = BaseEggLevel
            };
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
            private set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            private set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new SpiderMitesCard
            {
                BaseInfectLevel = BaseInfectLevel,
                BaseEggLevel = BaseEggLevel
            };
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
            private set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            private set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new FungusGnatsCard
            {
                BaseInfectLevel = BaseInfectLevel,
                BaseEggLevel = BaseEggLevel
            };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class DehydratedCard : IAfflictionCard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.DehydratedAffliction();
        public string Name => "Dehydrated";
        public int? Value => -3;
        private int _baseInfectLevel = 1;
        private int _baseEggLevel;

        public int BaseInfectLevel
        {
            get => _baseInfectLevel;
            private set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            private set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new DehydratedCard
            {
                BaseInfectLevel = BaseInfectLevel,
                BaseEggLevel = BaseEggLevel
            };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class NeedsLightCard : IAfflictionCard
    {
        public PlantAfflictions.IAffliction Affliction => new PlantAfflictions.NeedsLightAffliction();
        public string Name => "Needs Light";
        public int? Value => -3;
        private int _baseInfectLevel = 1;
        private int _baseEggLevel;

        public int BaseInfectLevel
        {
            get => _baseInfectLevel;
            private set => _baseInfectLevel = Mathf.Max(0, value);
        }

        public int BaseEggLevel
        {
            get => _baseEggLevel;
            private set => _baseEggLevel = Mathf.Max(0, value);
        }

        public List<ISticker> Stickers { get; } = new();

        public ICard Clone()
        {
            var clone = new NeedsLightCard
            {
                BaseInfectLevel = BaseInfectLevel,
                BaseEggLevel = BaseEggLevel
            };
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
        public ICard Clone()
        {
            var clone = new HorticulturalOilBasic { Value = Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class PermethrinBasic : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.PermethrinTreatment();
        public string Name => "Basic Permethrin Insecticide";
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
        public Material Material => Resources.Load<Material>($"Materials/Cards/Permethrin");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone()
        {
            var clone = new PermethrinBasic { Value = Value };
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
        public ICard Clone()
        {
            var clone = new FungicideBasic { Value = Value };
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
        public ICard Clone()
        {
            var clone = new SoapyWaterBasic { Value = Value };
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
        public ICard Clone()
        {
            var clone = new SpinosadTreatment { Value = Value };
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
        public ICard Clone()
        {
            var clone = new ImidaclopridTreatment { Value = Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class HydrationBasic : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.HydrationTreatmentBasic();
        public string Name => "HydrationBasic";
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
        public Material Material => Resources.Load<Material>($"Materials/Cards/Hydration");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone()
        {
            var clone = new HydrationBasic { Value = Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    public class SunlightBasic : ICard
    {
        [CanBeNull] private string _description;
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.SunlightTreatmentBasic();
        public string Name => "SunlightBasic";
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
        public Material Material => Resources.Load<Material>($"Materials/Cards/Sunlight");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone()
        {
            var clone = new SunlightBasic { Value = Value };
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
        public ICard Clone()
        {
            var clone = new Panacea { Value = Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }

    #endregion

    #region FieldSpells

    public class LadyBugsCard : IFieldSpell, ILocationCard
    {
        [CanBeNull] public string Description => "Lady Bugs be lady bugs bro...";
        public PlantAfflictions.ITreatment Treatment => new PlantAfflictions.LadyBugs();
        
        public int EffectDuration => IsPermanent ? 999 : 3;
        public bool IsPermanent => false;
        public LocationEffectType EffectType => null;
        
        public bool AffectsAllPlants { get; set; } = true;
        public bool ShowsGhosts { get; set; } = true;
        public bool TillDeath { get; set; } = true;
        
        public string Name => "Lady Bugs";
        
        private int _value = -15;
        public int? Value
        {
            get => _value;
            set => _value = value ?? 0;
        }

        public List<ISticker> Stickers { get; } = new();
        public GameObject Prefab => CardGameMaster.Instance.actionCardPrefab;
        public Material Material => Resources.Load<Material>($"Materials/Cards/LadyBugs");

        public void Selected() { if (CardGameMaster.Instance.debuggingCardClass) Debug.Log("Selected " + Name); }
        public void ModifyValue(int delta) => _value += delta;
        public ICard Clone()
        {
            var clone = new LadyBugsCard { Value = Value };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }

        
        public void ApplyLocationEffect(PlantController plant)
        {
            if (!plant) return;
            if (plant.CurrentTreatments.Any(t => t is PlantAfflictions.LadyBugs)) return;
            plant.CurrentTreatments.Add(new PlantAfflictions.LadyBugs());
        }

        public void RemoveLocationEffect(PlantController plant)
        {
            if (!plant) return;
            plant.CurrentTreatments.RemoveAll(t => t is PlantAfflictions.LadyBugs);

            if (CardGameMaster.Instance && CardGameMaster.Instance.debuggingCardClass)
            {
                Debug.Log($"Lady Bugs removed from {plant.name}");
            }
        }

        public void ApplyTurnEffect(PlantController plant)
        {
            // re-apply Lady Bugs treatment each turn
            plant.CurrentTreatments.Add(new PlantAfflictions.LadyBugs());
        }
    }

    #endregion
}
