using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Stickers
{
    [CreateAssetMenu(menuName = "Stickers/Copy Card")]
    public class CopyCardSticker : StickerDefinition
    {
        public override void Apply(ICard card)
        {
            base.Apply(card);
            CardGameMaster.Instance.deckManager.AddCardToHand(card.Clone());
        }
    }
}