using System;
using UnityEngine;

namespace _project.Scripts.Classes
{
    public class StickerClasses
    {
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

            public void Selected() { }
        }
    }
}