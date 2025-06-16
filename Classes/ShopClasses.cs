using System;
using _project.Scripts.Card_Core;
using UnityEngine;

namespace _project.Scripts.Classes
{
    public interface IShopItem
    {
        public ICard Card { get; }
        public string DisplayName { get; }
        public int Cost { get; }
        
        public void Purchase();
    }
    
    public class CardShopItem : IShopItem
    {
        public ICard Card { get; }
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
}