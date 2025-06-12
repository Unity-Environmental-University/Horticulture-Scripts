using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class ShopObject : MonoBehaviour
    {
        private ICard _card;
        private ShopManager _shopManager;

        public void Setup(ICard card, ShopManager shopManager)
        {
            _card = card;
            _shopManager = shopManager;
        }
    }
}