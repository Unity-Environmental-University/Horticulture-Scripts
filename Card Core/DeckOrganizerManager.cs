using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.UI;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class DeckOrganizerManager : MonoBehaviour
    {
        [SerializeField] private GameObject actionDeckItemsParent;
        [SerializeField] private GameObject actionDeckItemPrefab;
        [SerializeField] private GameObject sideDeckItemsParent;
        [SerializeField] private GameObject sideDeckItemPrefab;
        public GameObject deckUIPanel;

        // ReSharper disable twice CollectionNeverQueried.Local
        private readonly List<IShopItem> displayedActionCards = new();
        private readonly List<IShopItem> displayedSideCards = new();
        private DeckManager _deckManager;

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
        }

        public void OpenDeckOrganizer()
        {
            LoadActionDeck();
            LoadSideDeck();

            deckUIPanel.SetActive(true);
            Click3D.Click3DGloballyDisabled = true;
            UIInputManager.RequestEnable("DeckOrganizerManager");
        }

        public void CloseDeckOrganizer()
        {
            deckUIPanel.SetActive(false);
            Click3D.Click3DGloballyDisabled = false;
            UIInputManager.RequestDisable("DeckOrganizerManager");

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

        private void LoadActionDeck()
        {
            ClearOrganizer();

            var availableActionCards = _deckManager.GetActionDeck().ToList();

            foreach (var card in availableActionCards)
            {
                var cardObj = Instantiate(actionDeckItemPrefab, actionDeckItemsParent.transform);
                var itemLogic = new CardShopItem(card, _deckManager, cardObj);
                displayedActionCards.Add(itemLogic);

                var ui = cardObj.GetComponent<ShopObject>();
                ui.Setup(itemLogic);
            }
        }

        private void LoadSideDeck()
        {
            ClearOrganizer();

            var availableSideCards = _deckManager.GetSideDeck().ToList();

            foreach (var card in availableSideCards)
            {
                var cardObj = Instantiate(sideDeckItemPrefab, sideDeckItemsParent.transform);
                var itemLogic = new CardShopItem(card, _deckManager, cardObj);
                displayedSideCards.Add(itemLogic);

                var ui = cardObj.GetComponent<ShopObject>();
                ui.Setup(itemLogic);
            }
        }

        private void ClearOrganizer()
        {
            foreach (Transform child in actionDeckItemPrefab.transform)
                RemoveOrganizerItem(child.gameObject);
            foreach (Transform child in sideDeckItemPrefab.transform)
                RemoveOrganizerItem(child.gameObject);
        }

        private void RemoveOrganizerItem(GameObject deckUIItem)
        {
            var deckUIObject = deckUIItem.GetComponent<ShopObject>();
            if (!deckUIObject) return;

            displayedActionCards.Remove(deckUIObject.ShopItem);
            displayedSideCards.Remove(deckUIObject.ShopItem);
            Destroy(deckUIItem);
        }
    }
}