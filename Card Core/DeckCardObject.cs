using _project.Scripts.Classes;
using TMPro;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class DeckCardObject : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;

        public IShopItem ShopItem { get; private set; }

        public void Setup(IShopItem shopItem)
        {
            ShopItem = shopItem;

            titleText.text = ShopItem.DisplayName;
        }
    }
}