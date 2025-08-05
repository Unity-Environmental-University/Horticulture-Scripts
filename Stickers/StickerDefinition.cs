using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Stickers
{
    [CreateAssetMenu(menuName = "Stickers/Sticker Definition")]
    public class StickerDefinition : ScriptableObject, ISticker
    {
        [Header("Identity")]
        public string stickerName;
        [TextArea]
        public string description;

        [Header("Visuals")]
        public GameObject prefab;
        public Material material;

        [Header("Behavior")]
        [Tooltip("If true, applying this sticker will also add a copy of the card to hand.")]
        public bool copyOnApply;

        [Header("Data")]
        public int value;

        public string Name => stickerName;
        public string Description => description;
        public int? Value
        {
            get => value;
            set => this.value = value ?? 0;
        }
        public GameObject Prefab => prefab;
        public Material Material => material;

        public ISticker Clone()
        {
            // Definitions are immutable data assets; return this instance.
            return this;
        }

        public virtual void Selected() { }
        public virtual void Apply(ICard card)
        {
            card.ApplySticker(this);
            if (copyOnApply)
                CardGameMaster.Instance.deckManager.AddCardToHand(card.Clone());
        }
        public virtual void Peel(ICard card){ }
    }
}
