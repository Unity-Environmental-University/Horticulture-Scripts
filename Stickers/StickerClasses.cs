using System;
using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Stickers
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
        void Apply(ICard card) { throw new NotImplementedException(); }
        void Peel(ICard card) { throw new NotImplementedException(); }
    }

    #endregion

}
