using System;
using _project.Scripts.Card_Core;
using JetBrains.Annotations;
using UnityEngine;

namespace _project.Scripts.Classes
{
    public interface IShopItem
    {
        // ReSharper disable once UnusedMemberInSuper.Global
        [CanBeNull] public ICard Card { get; }
        [CanBeNull] public Material DisplayMaterial { get; }
        public string DisplayName { get; }
        public int Cost { get; }
        public void Purchase();
    }

    public enum UpgradeDuration
    {
        OneRound,    // Cleared at the end of round
        OneLevel     // Cleared at the end of level (all 5 rounds)
    }

    public interface IEnvironmentUpgrade : IShopItem
    {
        public GameObject Prefab { get; }
        public Material IconMaterial { get; }
        public UpgradeDuration Duration { get; }

        [CanBeNull]
        IBonus CalculateRoundBonus(int healthyPlantCount, int totalPlantCount);
    }

    /// <summary>
    ///     Shop item implementation for environment upgrades.
    ///     Wraps IEnvironmentUpgrade to be displayed and purchased in the shop.
    /// </summary>
    public class EnvironmentUpgradeShopItem : IShopItem
    {
        private readonly GameObject _gameObject;
        private readonly IEnvironmentUpgrade _upgrade;

        public EnvironmentUpgradeShopItem(IEnvironmentUpgrade upgrade, GameObject gameObject)
        {
            _upgrade = upgrade;
            _gameObject = gameObject;
        }

        // Environment upgrades are not cards
        public ICard Card => null;

        public Material DisplayMaterial => _upgrade.IconMaterial;

        public string DisplayName => _upgrade.DisplayName;

        public int Cost => _upgrade.Cost;

        public void Purchase()
        {
            _upgrade.Purchase();
            CardGameMaster.Instance.shopManager.RemoveShopItem(_gameObject);
        }
    }
    
    public class CardShopItem : IShopItem
    {
        public ICard Card { get; }
        public Material DisplayMaterial => Card.Material;
        public string DisplayName => Card.Name;
        public int Cost => Math.Abs(Card.Value ?? 0);

        private readonly DeckManager _deckManager;
        private GameObject GameObject { get; }

        public CardShopItem(ICard card, DeckManager deckManager, GameObject gameObject)
        {
            Card = card;
            _deckManager = deckManager;
            GameObject = gameObject;
        }

        public void Purchase()
        {
            _deckManager.AddActionCard(Card.Clone());
            ScoreManager.SubtractMoneys(Cost);
            CardGameMaster.Instance.shopManager.RemoveShopItem(GameObject);
        }
    }

    public class BeeBox : IEnvironmentUpgrade
    {
        public ICard Card => null;
        public Material DisplayMaterial => IconMaterial;
        public GameObject Prefab => Resources.Load<GameObject>($"Prefabs/Upgrades/BeeBox");
        public Material IconMaterial => Resources.Load<Material>($"Materials/Upgrades/BeeBoxIcon");

        public string DisplayName => "Bee Box";
        public string Description => "Pollination boost: +$2 per healthy plant this round";
        public int Cost => 15;
        public static int BonusPerPlant => 2;
        public UpgradeDuration Duration => UpgradeDuration.OneRound;

        public IBonus CalculateRoundBonus(int healthyPlantCount, int totalPlantCount)
        {
            var value = healthyPlantCount * BonusPerPlant;
            return value > 0 ? new IBonus { Name = "Bee Box", BonusValue = value } : null;
        }

        public void Purchase()
        {
            if (!CardGameMaster.Instance)
            {
                Debug.LogError("[BeeBox] Purchase failed: CardGameMaster not initialized!");
                return;
            }

            var manager = CardGameMaster.Instance.environmentUpgradeManager;
            if (!manager)
            {
                Debug.LogError("[BeeBox] Purchase failed: EnvironmentUpgradeManager not found!");
                return;
            }

            manager.PurchaseUpgrade(this);
            ScoreManager.SubtractMoneys(Cost);
        }
    }
}