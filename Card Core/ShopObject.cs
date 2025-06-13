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

        private IShopItem item;

        public void Setup(IShopItem shopItem)
        {
            item = shopItem;

            titleText.text = item.DisplayName;
            costText.text = "-$" + item.Cost;

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => item.Purchase());
        }
    }
}