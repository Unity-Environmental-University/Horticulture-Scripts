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

    public interface IEnvironmentUpgrade : IShopItem
    {
       public GameObject GameObject { get; }
       public Material IconMaterial { get; }
       [CanBeNull] public IBonus  Bonus { get; }
    }
    
    public class CardShopItem : IShopItem
    {
        public ICard Card { get; }
        public Material DisplayMaterial => null;
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
        public Material DisplayMaterial => null;
        public GameObject GameObject => null;
        public Material IconMaterial => null;
        public IBonus Bonus => new()
        {
            Name =  "BeeBox",
            BonusValue = 2
        };
        
        public string DisplayName => "Bee Box";
        public int Cost => 10;
        public void Purchase()
        {
            throw new NotImplementedException();
        }
    }
}