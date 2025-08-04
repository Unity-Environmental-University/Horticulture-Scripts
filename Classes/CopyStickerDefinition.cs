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
            var targetCard = dm.stickerTarget.GetComponentInParent<ICard>();
            Debug.LogError(targetCard);
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
