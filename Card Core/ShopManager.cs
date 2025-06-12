using System;
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
        public bool isShopOpen;

        private List<ICard> availableCards;

        private void Start() { GenerateShopInventory(); }

        public void OpenShop()
        {
            shopPanel.SetActive(true);
            isShopOpen = true;
        }

        public void CloseShop()
        {
            shopPanel.SetActive(false);
            isShopOpen = false;
        }

        private void GenerateShopInventory()
        {
            ClearShop();

            availableCards = new List<ICard>
            {
                new NeemOilBasic(),
                new SoapyWaterBasic(),
                new FungicideBasic(),
                new InsecticideBasic(),
                new Panacea()
            };

            for (var i = 0; i < numberOfCards; i++)
            {
                var randCard = availableCards[Random.Range(0, availableCards.Count)].Clone();
                var cardObj = Instantiate(shopItemPrefab, shopItemsParent.transform);
                var cardView = cardObj.GetComponent<CardView>();
                cardView.Setup(randCard);

                var shopItem = cardObj.AddComponent<ShopObject>();
                shopItem.Setup(randCard, this);
            }
        }

        private static void ClearShop() { throw new NotImplementedException(); }
    }
}