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
        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<IShopItem> currentShopItems = new();
        private List<ICard> availableCards;

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
            GenerateShopInventory();
        }

        public void OpenShop()
        {
            shopPanel.SetActive(true);
            Click3D.click3DGloballyDisabled = true;
            inputModule.enabled = true;
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
            
            //TODO Remove this For Implementation
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
                var itemLogic = new CardShopItem(randCard, _deckManager);
                currentShopItems.Add(itemLogic);

                var cardObj = Instantiate(shopItemPrefab, shopItemsParent.transform);
                var ui = cardObj.GetComponent<ShopObject>();
                ui.Setup(itemLogic);
            }
        }

        private void ClearShop()
        {
            foreach (Transform child in shopItemsParent.transform)
                Destroy(child.gameObject);
            currentShopItems.Clear();
        }
    }
}