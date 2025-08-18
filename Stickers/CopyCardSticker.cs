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
            var cloned = card.Clone();
            HandleClonedCard(cloned);
        }

        // Test seam: allows tests to intercept cloned card without requiring runtime singletons.
        protected virtual void HandleClonedCard(ICard cloned)
        {
            CardGameMaster.Instance.deckManager.AddCardToHandWithAnimation(cloned);
        }
    }
}