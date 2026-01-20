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
        [SerializeField] private GameObject cardDeckItemPrefab;
        [SerializeField] private GameObject sideDeckItemsParent;
        [SerializeField] private GameObject deckUIPanel;

        // ReSharper disable twice CollectionNeverQueried.Local
        private readonly List<IShopItem> displayedActionCards = new();
        private readonly List<IShopItem> displayedSideCards = new();
        private DeckManager _deckManager;

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
        }

        #region Open/Close

        public void OpenDeckOrganizer()
        {
            // Clear - then load
            ClearOrganizer();
            LoadActionDeck();
            LoadSideDeck();

            deckUIPanel.SetActive(true);
            Click3D.Click3DGloballyDisabled = true;
            UIInputManager.RequestEnable("DeckOrganizerManager");
        }

        public void CloseDeckOrganizer()
        {
            SaveActionDeck();
            SaveSideDeck();

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

        #endregion

        #region Loading

        private void LoadActionDeck()
        {
            _deckManager ??= CardGameMaster.Instance.deckManager;
            var availableActionCards = _deckManager.GetActionDeck().ToList();

            foreach (var card in availableActionCards)
            {
                var cardObj = Instantiate(cardDeckItemPrefab, actionDeckItemsParent.transform);
                var itemLogic = new CardShopItem(card, _deckManager, cardObj);
                displayedActionCards.Add(itemLogic);

                var ui = cardObj.GetComponent<DeckCardObject>();
                ui.Setup(itemLogic);
            }
        }

        private void LoadSideDeck()
        {
            _deckManager ??= CardGameMaster.Instance.deckManager;
            var availableSideCards = _deckManager.GetSideDeck().ToList();

            foreach (var card in availableSideCards)
            {
                var cardObj = Instantiate(cardDeckItemPrefab, sideDeckItemsParent.transform);
                var itemLogic = new CardShopItem(card, _deckManager, cardObj);
                displayedSideCards.Add(itemLogic);

                var ui = cardObj.GetComponent<DeckCardObject>();
                ui.Setup(itemLogic);
            }
        }

        #endregion

        #region Saving

        private void SaveActionDeck()
        {
            var modifiedActionDeck = actionDeckItemsParent.GetComponentsInChildren<DeckCardObject>();
            var newActionDeck = modifiedActionDeck.Select(dco => dco.ShopItem.Card).ToList();

            if (newActionDeck.Count > 0)
                _deckManager.SetActionDeck(newActionDeck);
            else
                Debug.LogError("ActionDeck is empty!");
        }

        private void SaveSideDeck()
        {
            var modifiedSideDeck = sideDeckItemsParent.GetComponentsInChildren<DeckCardObject>();
            var newSideDeck = modifiedSideDeck.Select(dco => dco.ShopItem.Card).ToList();

            if (newSideDeck.Count > 0)
                _deckManager.SetSideDeck(newSideDeck);
            else
                Debug.LogError("SideDeck is empty!");
        }

        #endregion

        #region Clean Up

        private void ClearOrganizer()
        {
            ClearOrganizerItems(actionDeckItemsParent);
            ClearOrganizerItems(sideDeckItemsParent);
        }

        private void ClearOrganizerItems(GameObject itemsParent)
        {
            if (itemsParent is null) return;

            for (var i = itemsParent.transform.childCount - 1; i >= 0; i--)
            {
                var child = itemsParent.transform.GetChild(i);
                RemoveOrganizerItem(child.gameObject);
            }
        }

        private void RemoveOrganizerItem(GameObject deckUIItem)
        {
            if (deckUIItem is null) return;

            var deckUIObject = deckUIItem.GetComponent<DeckCardObject>();
            if (deckUIObject is not null)
            {
                displayedActionCards.Remove(deckUIObject.ShopItem);
                displayedSideCards.Remove(deckUIObject.ShopItem);
            }

            deckUIItem.SetActive(false);
            Destroy(deckUIItem);
        }

        #endregion
    }
}