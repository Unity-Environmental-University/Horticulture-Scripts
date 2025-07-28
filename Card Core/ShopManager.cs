using System.Collections.Generic;
using _project.Scripts.Classes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _project.Scripts.Card_Core
{
    public class ShopManager : MonoBehaviour
    {
        [SerializeField] private GameObject shopItemsParent;
        [SerializeField] private GameObject shopItemPrefab;
        [SerializeField] private int numberOfCards = 4;

        public GameObject shopPanel;

        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<IShopItem> currentShopItems = new();
        private List<ICard> availableCards;
        private DeckManager _deckManager;

        private void Start() { _deckManager = CardGameMaster.Instance.deckManager; }

        public void OpenShop()
        {
            GenerateShopInventory();
            shopPanel.SetActive(true);
            Click3D.click3DGloballyDisabled = true;
            CardGameMaster.Instance.uiInputModule.enabled = true;
        }

        public void CloseShop()
        {
            shopPanel.SetActive(false);
            Click3D.click3DGloballyDisabled = false;
            CardGameMaster.Instance.uiInputModule.enabled = false;

            if (CardGameMaster.Instance.turnController.level == 2)
                CardGameMaster.Instance.turnController.ShowBetaScreen();
        }

        private void GenerateShopInventory()
        {
            ClearShop();

            availableCards = new List<ICard>
            {
                new HorticulturalOilBasic(),
                new SoapyWaterBasic(),
                new FungicideBasic(),
                new InsecticideBasic(),
                new Panacea()
            };

            for (var i = 0; i < numberOfCards; i++)
            {
                var randCard = availableCards[Random.Range(0, availableCards.Count)].Clone();
                var cardObj = Instantiate(shopItemPrefab, shopItemsParent.transform);
                var itemLogic = new CardShopItem(randCard, _deckManager, cardObj);
                currentShopItems.Add(itemLogic);

                var ui = cardObj.GetComponent<ShopObject>();
                ui.Setup(itemLogic);
            }
        }

        private void ClearShop()
        {
            foreach (Transform child in shopItemsParent.transform)
                RemoveShopItem(child.gameObject);
        }

        public void RemoveShopItem(GameObject shopItem)
        {
            var shopObject = shopItem.GetComponent<ShopObject>();
            if (!shopObject) return;

            currentShopItems.Remove(shopObject.ShopItem);
            Destroy(shopItem);
        }
    }
}