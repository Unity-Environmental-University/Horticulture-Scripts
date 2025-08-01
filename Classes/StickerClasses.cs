using System;
using _project.Scripts.Card_Core;
using UnityEngine;

namespace _project.Scripts.Classes
{
    #region Declairation

    public interface ISticker
    {
        string Name { get; }
        string Description => null;

        int? Value
        {
            get => null;
            set => throw new NotImplementedException();
        }

        GameObject Prefab => null;
        Material Material => null;
        ISticker Clone();

        void Selected() { throw new NotImplementedException(); }
        void Apply() { throw new NotImplementedException(); }
        void RunEffect() { throw new NotImplementedException(); }
        void Peel(ICard card) { throw new NotImplementedException(); }
    }

    #endregion

    #region CardStickers

    public class CopySticker : ISticker
    {
        public string Name => "";
        public string Description => "";
        public int Value => 0;
        
        public ISticker Clone() { return new CopySticker(); }

        public void Apply(ICard subjectCard)
        {
            subjectCard.ApplySticker(this);
            RunEffect(this, subjectCard);
        }

        private static void RunEffect(ISticker sticker, ICard card)
        {
            CardGameMaster.Instance.deckManager.AddCardToHand(card.Clone());
            sticker.Peel(card);
        }

        public void Peel(ICard card)
        {
            card.Stickers.Remove(this);
        }
    }

    #endregion
}