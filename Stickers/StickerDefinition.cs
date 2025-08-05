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

        [Header("Data")]
        public int value;

        public virtual string Name => stickerName;
        public virtual string Description => description;
        public virtual int? Value
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
        }
        public virtual void Peel(ICard card){ }
    }
}
