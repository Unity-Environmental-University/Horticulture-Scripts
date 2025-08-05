using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Stickers
{
    [CreateAssetMenu(menuName = "Stickers/Value Reducer")]
    public class ValueReducerSticker : StickerDefinition
    {
        [Header("Reducer Settings")] [Tooltip("Subtract this amount from the card's value when applied.")]
        public int reductionAmount = 1;

        public override void Apply(ICard card)
        {
            base.Apply(card);
            var current = card.Value ?? 0;
            switch (current)
            {
                case > 0:
                    card.ModifyValue(-reductionAmount);
                    break;
                case < 0:
                    card.ModifyValue(reductionAmount);
                    break;
            }
        }
    }
}