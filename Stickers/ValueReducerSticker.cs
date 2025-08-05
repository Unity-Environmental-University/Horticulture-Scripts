using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Stickers
{
    [CreateAssetMenu (menuName = "Stickers/Value Reducer")]
    public class ValueReducerSticker : StickerDefinition
    {
        public int reductionAmount = 1;

        public override void Apply(ICard card)
        {
            base.Apply(card);

            if (card.Value == null) return;
            var cValue = card.Value.Value;
            card.Value = cValue - reductionAmount;
        }
    }
}