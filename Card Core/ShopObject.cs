using _project.Scripts.Classes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _project.Scripts.Card_Core
{
    public class ShopObject : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button buyButton;

        public IShopItem ShopItem { get; private set; }

        public void Setup(IShopItem shopItem)
        {
            ShopItem = shopItem;

            titleText.text = ShopItem.DisplayName;
            costText.text = "-$" + ShopItem.Cost;

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => ShopItem.Purchase());
        }
    }
}