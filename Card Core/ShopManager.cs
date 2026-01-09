using System.Collections.Generic;
using _project.Scripts.Classes;
using _project.Scripts.UI;
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
            Click3D.Click3DGloballyDisabled = true;
            UIInputManager.RequestEnable("ShopManager");
        }

        public void CloseShop()
        {
            shopPanel.SetActive(false);
            Click3D.Click3DGloballyDisabled = false;
            UIInputManager.RequestDisable("ShopManager");

            var tc = CardGameMaster.Instance.turnController;
            if (tc.level == 2)
            {
                tc.ShowBetaScreen();
                return;
            }

            // Proceed to the next level sequence immediately after closing the shop
            tc.canClickEnd = false;
            tc.newRoundReady = false;
            StartCoroutine(tc.BeginTurnSequence());
        }

        private void GenerateShopInventory()
        {
            ClearShop();

            availableCards = new List<ICard>
            {
                new HorticulturalOilBasic(),
                new SoapyWaterBasic(),
                new FungicideBasic(),
                new PermethrinBasic(),
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
