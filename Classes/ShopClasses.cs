using System;
using _project.Scripts.Card_Core;

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

        public CardShopItem(ICard card, DeckManager deckManager)
        {
            Card = card;
            _deckManager = deckManager;
        }

        public void Purchase()
        {
            _deckManager.AddActionCard(Card.Clone());
            ScoreManager.SubtractMoneys(Cost);
        }
    }
}