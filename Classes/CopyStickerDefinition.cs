using _project.Scripts.Card_Core;
using UnityEngine;

namespace _project.Scripts.Classes
{
    [CreateAssetMenu(menuName = "Stickers/Copy Sticker")]
    public class CopyStickerDefinition : StickerDefinition
    {
        public override void Apply()
        {
            // Example: add a cloned card to hand and peel this sticker
            if (CardGameMaster.Instance?.deckManager == null) return;
            // Assuming the last applied card is passed in context; adapt as needed
            var targetCard = CardGameMaster.Instance.deckManager.SelectedACard;
            if (targetCard == null) return;
            targetCard.ApplySticker(this);
            CardGameMaster.Instance.deckManager.AddCardToHand(targetCard.Clone());
            //Peel(targetCard);
        }

        public override void Peel(ICard card)
        {
            card.Stickers.Remove(this);
        }
    }
}
