using _project.Scripts.Card_Core;
using UnityEngine;

namespace _project.Scripts.Classes
{
    [CreateAssetMenu(menuName = "Stickers/Copy Sticker")]
    public class CopyStickerDefinition : StickerDefinition
    {
        public override void Apply()
        {
            var dm = CardGameMaster.Instance?.deckManager;
            if (dm == null) return;
            var cardView = dm.stickerTarget.GetComponentInParent<CardView>();
            if (cardView == null) return;
            var targetCard = cardView.GetCard();
            //Debug.LogError(targetCard);
            if (targetCard == null) return;
            targetCard.ApplySticker(this);
            dm.AddCardToHand(targetCard.Clone());
        }

        public override void Peel(ICard card)
        {
            card.Stickers.Remove(this);
        }
    }
}
