using System;
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

        public void Selected() { throw new NotImplementedException(); }
        public void Apply() { throw new NotImplementedException(); }
    }

    #endregion

    #region CardStickers

    public class CopySticker : ISticker
    {
        public string Name => "";
        public string Description => "";
        public int Value => 0;
        
        public ISticker Clone() { return new CopySticker(); }
    }

    #endregion
}