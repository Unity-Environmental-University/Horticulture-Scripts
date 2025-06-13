using _project.Scripts.Classes;
using TMPro;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class ShopObject : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI itemCostText;
        private ICard _card;
        private ShopManager _shopManager;

        public void Setup(ICard card, ShopManager shopManager)
        {
            _card = card;
            _shopManager = shopManager;

            titleText.text = _card.Name;
            if (card.Value != null) itemCostText.text = "-$" + card.Value.Value;
        }
    }
}