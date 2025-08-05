using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Stickers
{
    [CreateAssetMenu (menuName = "Stickers/Value Reducer")]
    public class ValueReducerSticker : StickerDefinition
    {
        [Header("Reducer Settings")]
        [Tooltip("Subtract this amount from the card's value when applied.")]
        public int reductionAmount = 1;

        public override void Apply(ICard card)
        {
            base.Apply(card);
            card.ModifyValue(-reductionAmount);
            Debug.LogError(card.Value.Value);
        }
    }
}