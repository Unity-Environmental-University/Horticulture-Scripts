using System;
using System.Collections.Generic;
using _project.Scripts.Classes;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using Random = UnityEngine.Random;

namespace _project.Scripts.Card_Core
{
    public class ShopManager : MonoBehaviour
    {
        [SerializeField] private GameObject shopItemsParent;
        [SerializeField] private GameObject shopItemPrefab;
        [SerializeField] private InputSystemUIInputModule  inputModule;
        [SerializeField] private int numberOfCards = 4;

        public GameObject shopPanel;
        public bool isShopOpen;

        private DeckManager _deckManager;
        private List<ICard> availableCards;

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
            GenerateShopInventory();
        }

        public void OpenShop()
        {
            shopPanel.SetActive(true);
            isShopOpen = true;
        }

        public void CloseShop()
        {
            shopPanel.SetActive(false);
            Click3D.click3DGloballyDisabled = false;
            inputModule.enabled = false;
            isShopOpen = false;
        }

        private void GenerateShopInventory()
        {
            ClearShop();
            
            //TODO Move this to Open
            Click3D.click3DGloballyDisabled = true;
            inputModule.enabled = true;
            //
            
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
                var shopItem =
                    cardObj.GetComponent<ShopObject>() ?? cardObj.AddComponent<ShopObject>();
                
                shopItem.Setup(randCard, this);
            }
        }

        public void PurchaseCard(ICard card)
        {
            var cost = Math.Abs(card.Value ?? 0);
            if (ScoreManager.GetMoneys() >= cost)
            {
                _deckManager.AddActionCard(card.Clone());
                ScoreManager.SubtractMoneys(cost);
            }
            else
            {
                Debug.LogWarning($"Not enough money for {card.Name}");
            }
        }

        private void ClearShop()
        {
            foreach (Transform child in shopItemsParent.transform)
                Destroy(child.gameObject);
        }
    }
}