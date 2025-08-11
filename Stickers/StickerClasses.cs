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
            set { }
        }

        GameObject Prefab => null;
        Material Material => null;
        ISticker Clone();

        void Selected() { }
        void Apply(ICard card) 
        { 
            card?.ApplySticker(this);
        }
        void Peel(ICard card) { }
    }

    #endregion

}
