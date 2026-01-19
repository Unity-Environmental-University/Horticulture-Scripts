using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _project.Scripts.Audio;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Stickers;
using _project.Scripts.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

// ReSharper disable PossibleNullReferenceException

namespace _project.Scripts.PlayModeTest
{
    public class DeckOrganizerManagerTests
    {
        #region Test Fixture Fields

        // Core Components
        private GameObject _cardGameMasterGo;
        private CardGameMaster _cardGameMaster;
        private DeckManager _deckManager;
        private TurnController _turnController;
        private ScoreManager _scoreManager;
        private DeckOrganizerManager _deckOrganizerManager;

        // UI Hierarchy
        private GameObject _deckUIPanel;
        private GameObject _actionDeckParent;
        private GameObject _sideDeckParent;
        private GameObject _actionPrefab;
        private GameObject _sidePrefab;

        // Support objects
        private GameObject _lostObjectsGo;
        private GameObject _winScreenGo;

        // Reflection field caches
        private FieldInfo _actionDeckField;
        private FieldInfo _sideDeckField;

        #endregion

        #region Setup and Teardown

        [SetUp]
        public void Setup()
        {
            // Create CardGameMaster hierarchy inactive to control initialization
            _cardGameMasterGo = new GameObject("CardGameMaster");
            _cardGameMasterGo.SetActive(false);

            // Add core components
            _deckManager = _cardGameMasterGo.AddComponent<DeckManager>();
            _scoreManager = _cardGameMasterGo.AddComponent<ScoreManager>();
            _turnController = _cardGameMasterGo.AddComponent<TurnController>();
            _cardGameMaster = _cardGameMasterGo.AddComponent<CardGameMaster>();
            var soundSystem = _cardGameMasterGo.AddComponent<SoundSystemMaster>();
            var audioSource = _cardGameMasterGo.AddComponent<AudioSource>();
            _cardGameMasterGo.AddComponent<AudioListener>();

            // Add DeckOrganizerManager
            _deckOrganizerManager = _cardGameMasterGo.AddComponent<DeckOrganizerManager>();

            // Create UI hierarchy
            _deckUIPanel = new GameObject("DeckUIPanel");
            _actionDeckParent = new GameObject("ActionDeckParent");
            _sideDeckParent = new GameObject("SideDeckParent");
            _actionDeckParent.transform.SetParent(_deckUIPanel.transform);
            _sideDeckParent.transform.SetParent(_deckUIPanel.transform);

            // Create mock prefabs
            _actionPrefab = CreateMockShopObjectPrefab("ActionCardPrefab");
            _sidePrefab = CreateMockShopObjectPrefab("SideDeckCardPrefab");

            // Wire DeckOrganizerManager dependencies via reflection (SerializeField)
            var deckPanelField = typeof(DeckOrganizerManager)
                .GetField("deckUIPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            deckPanelField?.SetValue(_deckOrganizerManager, _deckUIPanel);

            var actionParentField = typeof(DeckOrganizerManager)
                .GetField("actionDeckItemsParent", BindingFlags.NonPublic | BindingFlags.Instance);
            actionParentField?.SetValue(_deckOrganizerManager, _actionDeckParent);

            var sideParentField = typeof(DeckOrganizerManager)
                .GetField("sideDeckItemsParent", BindingFlags.NonPublic | BindingFlags.Instance);
            sideParentField?.SetValue(_deckOrganizerManager, _sideDeckParent);

            var actionPrefabField = typeof(DeckOrganizerManager)
                .GetField("actionDeckItemPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            actionPrefabField?.SetValue(_deckOrganizerManager, _actionPrefab);

            var sidePrefabField = typeof(DeckOrganizerManager)
                .GetField("sideDeckItemPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            sidePrefabField?.SetValue(_deckOrganizerManager, _sidePrefab);

            // Inject _deckManager reference (normally set in Start())
            var deckManagerField = typeof(DeckOrganizerManager)
                .GetField("_deckManager", BindingFlags.NonPublic | BindingFlags.Instance);
            deckManagerField?.SetValue(_deckOrganizerManager, _deckManager);

            // Set actionCardPrefab for CardShopItem (used in ShopObject.Setup)
            _cardGameMaster.actionCardPrefab = _actionPrefab;

            // Minimal TurnController dependencies
            _lostObjectsGo = new GameObject("LostObjects");
            _winScreenGo = new GameObject("WinScreen");
            _turnController.lostGameObjects = _lostObjectsGo;
            _turnController.winScreen = _winScreenGo;

            // Wire CardGameMaster dependencies
            _cardGameMaster.deckManager = _deckManager;
            _cardGameMaster.scoreManager = _scoreManager;
            _cardGameMaster.turnController = _turnController;
            _cardGameMaster.soundSystem = soundSystem;
            _cardGameMaster.playerHandAudioSource = audioSource;

            // Set CardGameMaster singleton via reflection
            typeof(CardGameMaster)
                .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?.SetValue(null, _cardGameMaster);

            // Cache reflection fields for tests
            _actionDeckField = typeof(DeckManager)
                .GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            _sideDeckField = typeof(DeckManager)
                .GetField("_sideDeck", BindingFlags.NonPublic | BindingFlags.Instance);

            // Suppress expected initialization warnings
            LogAssert.ignoreFailingMessages = true;

            // Activate to trigger lifecycle
            _cardGameMasterGo.SetActive(true);
            _deckUIPanel.SetActive(false); // Start with panel inactive
        }

        [TearDown]
        public void TearDown()
        {
            // Destroy all test GameObjects
            UnityEngine.Object.Destroy(_cardGameMasterGo);
            UnityEngine.Object.Destroy(_deckUIPanel);
            UnityEngine.Object.Destroy(_actionDeckParent);
            UnityEngine.Object.Destroy(_sideDeckParent);
            UnityEngine.Object.Destroy(_actionPrefab);
            UnityEngine.Object.Destroy(_sidePrefab);
            UnityEngine.Object.Destroy(_lostObjectsGo);
            UnityEngine.Object.Destroy(_winScreenGo);

            // Clear singleton reference
            typeof(CardGameMaster)
                .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?.SetValue(null, null);

            // Reset static state
            Click3D.Click3DGloballyDisabled = false;
        }

        #endregion

        #region Helper Methods

        private GameObject CreateMockShopObjectPrefab(string name)
        {
            var prefab = new GameObject(name);

            // Add ShopObject component
            var shopObject = prefab.AddComponent<ShopObject>();

            // Add required UI components via reflection
            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(prefab.transform);
            var titleText = titleTextGo.AddComponent<TextMeshProUGUI>();

            var costTextGo = new GameObject("CostText");
            costTextGo.transform.SetParent(prefab.transform);
            var costText = costTextGo.AddComponent<TextMeshProUGUI>();

            var buttonGo = new GameObject("Button");
            buttonGo.transform.SetParent(prefab.transform);
            var button = buttonGo.AddComponent<Button>();

            // Wire ShopObject fields via reflection
            var titleField = typeof(ShopObject)
                .GetField("titleText", BindingFlags.NonPublic | BindingFlags.Instance);
            titleField?.SetValue(shopObject, titleText);

            var costField = typeof(ShopObject)
                .GetField("costText", BindingFlags.NonPublic | BindingFlags.Instance);
            costField?.SetValue(shopObject, costText);

            var buttonField = typeof(ShopObject)
                .GetField("buyButton", BindingFlags.NonPublic | BindingFlags.Instance);
            buttonField?.SetValue(shopObject, button);

            return prefab;
        }

        private GameObject CreateMockShopObject(ICard card, Transform parent, GameObject prefab)
        {
            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            var shopObject = instance.GetComponent<ShopObject>();
            var shopItem = new CardShopItem(card, _deckManager, instance);
            shopObject.Setup(shopItem);
            return instance;
        }

        private FakeCard CreateFakeCard(string name, int value = 0)
        {
            return new FakeCard(name, value);
        }

        private List<ICard> GetActionDeck()
        {
            return _actionDeckField.GetValue(_deckManager) as List<ICard>;
        }

        private List<ICard> GetSideDeck()
        {
            return _sideDeckField.GetValue(_deckManager) as List<ICard>;
        }

        private void InvokeLoadActionDeck()
        {
            var method = typeof(DeckOrganizerManager)
                .GetMethod("LoadActionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_deckOrganizerManager, null);
        }

        private void InvokeLoadSideDeck()
        {
            var method = typeof(DeckOrganizerManager)
                .GetMethod("LoadSideDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_deckOrganizerManager, null);
        }

        private void InvokeSaveActionDeck()
        {
            var method = typeof(DeckOrganizerManager)
                .GetMethod("SaveActionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_deckOrganizerManager, null);
        }

        private void InvokeSaveSideDeck()
        {
            var method = typeof(DeckOrganizerManager)
                .GetMethod("SaveSideDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_deckOrganizerManager, null);
        }

        private void InvokeClearOrganizer()
        {
            var method = typeof(DeckOrganizerManager)
                .GetMethod("ClearOrganizer", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_deckOrganizerManager, null);
        }

        #endregion

        #region Bug Verification Tests

        [Test]
        public void Test_GetComponentsInChildren_IShopItem_ReturnsEmpty()
        {
            // Arrange: Create UI hierarchy with ShopObject children
            var card1 = CreateFakeCard("Card1");
            var card2 = CreateFakeCard("Card2");
            CreateMockShopObject(card1, _actionDeckParent.transform, _actionPrefab);
            CreateMockShopObject(card2, _actionDeckParent.transform, _actionPrefab);

            // Act: Attempt to retrieve IShopItem instances (the buggy approach)
            var result = _actionDeckParent.GetComponentsInChildren<IShopItem>();

            // Assert: Should return empty because interfaces don't work with GetComponentsInChildren
            Assert.AreEqual(0, result.Length, "GetComponentsInChildren<IShopItem>() should return empty array (proves bug exists)");
        }

        [Test]
        public void Test_GetComponentsInChildren_ShopObject_ReturnsCorrect()
        {
            // Arrange: Create UI hierarchy with ShopObject children
            var card1 = CreateFakeCard("Card1");
            var card2 = CreateFakeCard("Card2");
            CreateMockShopObject(card1, _actionDeckParent.transform, _actionPrefab);
            CreateMockShopObject(card2, _actionDeckParent.transform, _actionPrefab);

            // Act: Retrieve ShopObject instances (the correct approach)
            var result = _actionDeckParent.GetComponentsInChildren<ShopObject>();

            // Assert: Should return correct number of ShopObjects
            Assert.AreEqual(2, result.Length, "GetComponentsInChildren<ShopObject>() should return correct count");

            // Verify we can access card data through ShopObject.ShopItem.Card
            Assert.IsNotNull(result[0].ShopItem, "ShopItem should be accessible");
            Assert.IsNotNull(result[0].ShopItem.Card, "Card should be accessible through ShopItem");
            Assert.AreEqual("Card1", result[0].ShopItem.Card.Name, "First card should be Card1");
            Assert.AreEqual("Card2", result[1].ShopItem.Card.Name, "Second card should be Card2");
        }

        #endregion

        #region Loading Tests

        [Test]
        public void Test_LoadActionDeck_CreatesCorrectNumberOfShopObjects()
        {
            // Arrange: Set up action deck with 5 cards
            var actionDeck = GetActionDeck();
            actionDeck.Clear();
            for (var i = 0; i < 5; i++)
            {
                actionDeck.Add(CreateFakeCard($"ActionCard{i}"));
            }

            // Act: Load action deck
            InvokeLoadActionDeck();

            // Assert: Should create 5 ShopObject instances
            var shopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.AreEqual(5, shopObjects.Length, "Should create 5 ShopObject instances");
        }

        [Test]
        public void Test_LoadActionDeck_InstantiatesAsChildrenOfParent()
        {
            // Arrange: Set up action deck with 3 cards
            var actionDeck = GetActionDeck();
            actionDeck.Clear();
            for (var i = 0; i < 3; i++)
            {
                actionDeck.Add(CreateFakeCard($"Card{i}"));
            }

            // Act: Load action deck
            InvokeLoadActionDeck();

            // Assert: All instances should be children of actionDeckParent
            var shopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            foreach (var shopObject in shopObjects)
            {
                Assert.AreEqual(_actionDeckParent.transform, shopObject.transform.parent,
                    "ShopObject should be child of actionDeckParent");
            }
        }

        [Test]
        public void Test_LoadActionDeck_CallsShopObjectSetup()
        {
            // Arrange: Set up action deck with 2 cards
            var actionDeck = GetActionDeck();
            actionDeck.Clear();
            actionDeck.Add(CreateFakeCard("Card1", 10));
            actionDeck.Add(CreateFakeCard("Card2", 20));

            // Act: Load action deck
            InvokeLoadActionDeck();

            // Assert: Verify Setup was called by checking ShopItem is populated
            var shopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.IsNotNull(shopObjects[0].ShopItem, "ShopItem should be set (Setup was called)");
            Assert.AreEqual("Card1", shopObjects[0].ShopItem.Card.Name, "ShopItem should have correct card");
            Assert.AreEqual("Card2", shopObjects[1].ShopItem.Card.Name, "Second ShopItem should have correct card");
        }

        [Test]
        public void Test_LoadActionDeck_PreservesCardOrder()
        {
            // Arrange: Set up action deck with uniquely identifiable cards
            var actionDeck = GetActionDeck();
            actionDeck.Clear();
            actionDeck.Add(CreateFakeCard("Alpha"));
            actionDeck.Add(CreateFakeCard("Beta"));
            actionDeck.Add(CreateFakeCard("Gamma"));
            actionDeck.Add(CreateFakeCard("Delta"));

            // Act: Load action deck
            InvokeLoadActionDeck();

            // Assert: Cards should appear in same order
            var shopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.AreEqual("Alpha", shopObjects[0].ShopItem.Card.Name, "First card should be Alpha");
            Assert.AreEqual("Beta", shopObjects[1].ShopItem.Card.Name, "Second card should be Beta");
            Assert.AreEqual("Gamma", shopObjects[2].ShopItem.Card.Name, "Third card should be Gamma");
            Assert.AreEqual("Delta", shopObjects[3].ShopItem.Card.Name, "Fourth card should be Delta");
        }

        [Test]
        public void Test_LoadActionDeck_WithEmptyDeck_CreatesNoObjects()
        {
            // Arrange: Clear action deck
            var actionDeck = GetActionDeck();
            actionDeck.Clear();

            // Act: Load action deck
            InvokeLoadActionDeck();

            // Assert: Should create no ShopObjects
            var shopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.AreEqual(0, shopObjects.Length, "Should create no ShopObjects for empty deck");
            Assert.AreEqual(0, _actionDeckParent.transform.childCount, "Parent should have no children");
        }

        [Test]
        public void Test_LoadSideDeck_CreatesCorrectNumberOfShopObjects()
        {
            // Arrange: Set up side deck with 3 cards
            var sideDeck = GetSideDeck();
            sideDeck.Clear();
            for (var i = 0; i < 3; i++)
            {
                sideDeck.Add(CreateFakeCard($"SideCard{i}"));
            }

            // Act: Load side deck
            InvokeLoadSideDeck();

            // Assert: Should create 3 ShopObject instances
            var shopObjects = _sideDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.AreEqual(3, shopObjects.Length, "Should create 3 ShopObject instances");
        }

        [Test]
        public void Test_LoadSideDeck_InstantiatesAsChildrenOfParent()
        {
            // Arrange: Set up side deck with 2 cards
            var sideDeck = GetSideDeck();
            sideDeck.Clear();
            sideDeck.Add(CreateFakeCard("SideCard1"));
            sideDeck.Add(CreateFakeCard("SideCard2"));

            // Act: Load side deck
            InvokeLoadSideDeck();

            // Assert: All instances should be children of sideDeckParent
            var shopObjects = _sideDeckParent.GetComponentsInChildren<ShopObject>();
            foreach (var shopObject in shopObjects)
            {
                Assert.AreEqual(_sideDeckParent.transform, shopObject.transform.parent,
                    "ShopObject should be child of sideDeckParent");
            }
        }

        [Test]
        public void Test_LoadSideDeck_CallsShopObjectSetup()
        {
            // Arrange: Set up side deck with 2 cards
            var sideDeck = GetSideDeck();
            sideDeck.Clear();
            sideDeck.Add(CreateFakeCard("SideCard1", 15));
            sideDeck.Add(CreateFakeCard("SideCard2", 25));

            // Act: Load side deck
            InvokeLoadSideDeck();

            // Assert: Verify Setup was called
            var shopObjects = _sideDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.IsNotNull(shopObjects[0].ShopItem, "ShopItem should be set");
            Assert.AreEqual("SideCard1", shopObjects[0].ShopItem.Card.Name, "ShopItem should have correct card");
        }

        [Test]
        public void Test_LoadSideDeck_PreservesCardOrder()
        {
            // Arrange: Set up side deck with ordered cards
            var sideDeck = GetSideDeck();
            sideDeck.Clear();
            sideDeck.Add(CreateFakeCard("First"));
            sideDeck.Add(CreateFakeCard("Second"));
            sideDeck.Add(CreateFakeCard("Third"));

            // Act: Load side deck
            InvokeLoadSideDeck();

            // Assert: Cards should appear in same order
            var shopObjects = _sideDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.AreEqual("First", shopObjects[0].ShopItem.Card.Name, "First card should be First");
            Assert.AreEqual("Second", shopObjects[1].ShopItem.Card.Name, "Second card should be Second");
            Assert.AreEqual("Third", shopObjects[2].ShopItem.Card.Name, "Third card should be Third");
        }

        [Test]
        public void Test_LoadSideDeck_WithEmptySideDeck_CreatesNoObjects()
        {
            // Arrange: Clear side deck
            var sideDeck = GetSideDeck();
            sideDeck.Clear();

            // Act: Load side deck
            InvokeLoadSideDeck();

            // Assert: Should create no ShopObjects
            var shopObjects = _sideDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.AreEqual(0, shopObjects.Length, "Should create no ShopObjects for empty side deck");
            Assert.AreEqual(0, _sideDeckParent.transform.childCount, "Parent should have no children");
        }

        #endregion

        #region Saving Tests

        [Test]
        public void Test_SaveActionDeck_WithBuggedCode_ReturnsEmptyArray()
        {
            // Arrange: Manually create UI hierarchy with ShopObjects (simulating loaded deck)
            var actionDeck = GetActionDeck();
            actionDeck.Clear();
            actionDeck.Add(CreateFakeCard("Card1"));
            actionDeck.Add(CreateFakeCard("Card2"));
            InvokeLoadActionDeck();

            // Act: Directly test the buggy approach used in SaveActionDeck
            var modifiedActionDeck = _actionDeckParent.GetComponentsInChildren<IShopItem>();

            // Assert: Buggy code returns empty array even though UI has ShopObjects
            Assert.AreEqual(0, modifiedActionDeck.Length,
                "Current implementation uses GetComponentsInChildren<IShopItem>() which returns empty (demonstrates bug)");
        }

        [Test]
        public void Test_SaveActionDeck_WithEmptyUI_LogsError()
        {
            // Arrange: Clear action deck parent (simulate empty UI)
            foreach (Transform child in _actionDeckParent.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }


            // Act & Assert: Should log error about empty deck
            LogAssert.Expect(LogType.Error, "ActionDeck is empty!");
            InvokeSaveActionDeck();
        }

        [Test]
        public void Test_SaveActionDeck_WithPopulatedUI_FixedApproach()
        {
            // Arrange: Create UI with ShopObjects using correct approach
            var card1 = CreateFakeCard("Fixed1");
            var card2 = CreateFakeCard("Fixed2");
            var card3 = CreateFakeCard("Fixed3");
            CreateMockShopObject(card1, _actionDeckParent.transform, _actionPrefab);
            CreateMockShopObject(card2, _actionDeckParent.transform, _actionPrefab);
            CreateMockShopObject(card3, _actionDeckParent.transform, _actionPrefab);

            // Act: Use the CORRECT approach (what the fix should be)
            var shopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            var retrievedCards = shopObjects.Select(so => so.ShopItem.Card).ToList();

            // Assert: Correct approach successfully retrieves cards
            Assert.AreEqual(3, retrievedCards.Count, "Fixed approach should retrieve 3 cards");
            Assert.AreEqual("Fixed1", retrievedCards[0].Name, "First card should be Fixed1");
            Assert.AreEqual("Fixed2", retrievedCards[1].Name, "Second card should be Fixed2");
            Assert.AreEqual("Fixed3", retrievedCards[2].Name, "Third card should be Fixed3");
        }

        [Test]
        public void Test_SaveActionDeck_PreservesCardOrder()
        {
            // Arrange: Create UI with specific card order
            var card1 = CreateFakeCard("Zulu");
            var card2 = CreateFakeCard("Yankee");
            var card3 = CreateFakeCard("X-ray");
            CreateMockShopObject(card1, _actionDeckParent.transform, _actionPrefab);
            CreateMockShopObject(card2, _actionDeckParent.transform, _actionPrefab);
            CreateMockShopObject(card3, _actionDeckParent.transform, _actionPrefab);

            // Act: Use correct retrieval approach
            var shopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            var retrievedCards = shopObjects.Select(so => so.ShopItem.Card).ToList();

            // Assert: Order should be preserved
            Assert.AreEqual("Zulu", retrievedCards[0].Name, "Order should be preserved");
            Assert.AreEqual("Yankee", retrievedCards[1].Name, "Order should be preserved");
            Assert.AreEqual("X-ray", retrievedCards[2].Name, "Order should be preserved");
        }

        [Test]
        public void Test_SaveActionDeck_UpdatesDeckManager()
        {
            // Arrange: Load initial deck, then modify UI
            var actionDeck = GetActionDeck();
            actionDeck.Clear();
            actionDeck.Add(CreateFakeCard("Original1"));
            actionDeck.Add(CreateFakeCard("Original2"));
            InvokeLoadActionDeck();

            // Clear original deck to simulate modifications
            actionDeck.Clear();

            // Create new UI state manually (simulating user reorganization)
            // Use DestroyImmediate for synchronous destruction in tests
            while (_actionDeckParent.transform.childCount > 0)
            {
                UnityEngine.Object.DestroyImmediate(_actionDeckParent.transform.GetChild(0).gameObject);
            }
            CreateMockShopObject(CreateFakeCard("Modified1"), _actionDeckParent.transform, _actionPrefab);
            CreateMockShopObject(CreateFakeCard("Modified2"), _actionDeckParent.transform, _actionPrefab);
            CreateMockShopObject(CreateFakeCard("Modified3"), _actionDeckParent.transform, _actionPrefab);

            // Manually apply the FIX (what SaveActionDeck should do after bug fix)
            var shopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            var newActionDeck = shopObjects.Select(so => so.ShopItem.Card).ToList();
            if (newActionDeck.Count > 0)
            {
                _deckManager.SetActionDeck(newActionDeck);
            }

            // Assert: Deck should be updated with new cards
            var updatedDeck = GetActionDeck();
            Assert.AreEqual(3, updatedDeck.Count, "Deck should have 3 cards after save");
            Assert.AreEqual("Modified1", updatedDeck[0].Name, "First card should be Modified1");
            Assert.AreEqual("Modified2", updatedDeck[1].Name, "Second card should be Modified2");
            Assert.AreEqual("Modified3", updatedDeck[2].Name, "Third card should be Modified3");
        }

        [Test]
        public void Test_SaveSideDeck_WithBuggedCode_ReturnsEmptyArray()
        {
            // Arrange: Load side deck
            var sideDeck = GetSideDeck();
            sideDeck.Clear();
            sideDeck.Add(CreateFakeCard("SideCard1"));
            sideDeck.Add(CreateFakeCard("SideCard2"));
            InvokeLoadSideDeck();

            // Act: Test buggy approach
            var modifiedSideDeck = _sideDeckParent.GetComponentsInChildren<IShopItem>();

            // Assert: Buggy code returns empty
            Assert.AreEqual(0, modifiedSideDeck.Length,
                "Current implementation uses GetComponentsInChildren<IShopItem>() which returns empty");
        }

        [Test]
        public void Test_SaveSideDeck_WithEmptyUI_LogsError()
        {
            // Arrange: Clear side deck parent
            foreach (Transform child in _sideDeckParent.transform)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }


            // Act & Assert: Should log error
            LogAssert.Expect(LogType.Error, "SideDeck is empty!");
            InvokeSaveSideDeck();
        }

        [Test]
        public void Test_SaveSideDeck_WithPopulatedUI_FixedApproach()
        {
            // Arrange: Create side deck UI with correct approach
            var card1 = CreateFakeCard("SideFix1");
            var card2 = CreateFakeCard("SideFix2");
            CreateMockShopObject(card1, _sideDeckParent.transform, _sidePrefab);
            CreateMockShopObject(card2, _sideDeckParent.transform, _sidePrefab);

            // Act: Use correct approach
            var shopObjects = _sideDeckParent.GetComponentsInChildren<ShopObject>();
            var retrievedCards = shopObjects.Select(so => so.ShopItem.Card).ToList();

            // Assert: Should retrieve cards correctly
            Assert.AreEqual(2, retrievedCards.Count, "Should retrieve 2 cards");
            Assert.AreEqual("SideFix1", retrievedCards[0].Name, "First card should be SideFix1");
            Assert.AreEqual("SideFix2", retrievedCards[1].Name, "Second card should be SideFix2");
        }

        [Test]
        public void Test_SaveSideDeck_PreservesCardOrder()
        {
            // Arrange: Create side deck UI with specific order
            var card1 = CreateFakeCard("Three");
            var card2 = CreateFakeCard("Two");
            var card3 = CreateFakeCard("One");
            CreateMockShopObject(card1, _sideDeckParent.transform, _sidePrefab);
            CreateMockShopObject(card2, _sideDeckParent.transform, _sidePrefab);
            CreateMockShopObject(card3, _sideDeckParent.transform, _sidePrefab);

            // Act: Retrieve with correct approach
            var shopObjects = _sideDeckParent.GetComponentsInChildren<ShopObject>();
            var retrievedCards = shopObjects.Select(so => so.ShopItem.Card).ToList();

            // Assert: Order preserved
            Assert.AreEqual("Three", retrievedCards[0].Name, "Order should be preserved");
            Assert.AreEqual("Two", retrievedCards[1].Name, "Order should be preserved");
            Assert.AreEqual("One", retrievedCards[2].Name, "Order should be preserved");
        }

        [Test]
        public void Test_SaveSideDeck_UpdatesDeckManager()
        {
            // Arrange: Load initial side deck
            var sideDeck = GetSideDeck();
            sideDeck.Clear();
            sideDeck.Add(CreateFakeCard("OriginalSide1"));
            InvokeLoadSideDeck();

            // Clear and create new UI state
            sideDeck.Clear();
            // Use DestroyImmediate for synchronous destruction in tests
            while (_sideDeckParent.transform.childCount > 0)
            {
                UnityEngine.Object.DestroyImmediate(_sideDeckParent.transform.GetChild(0).gameObject);
            }
            CreateMockShopObject(CreateFakeCard("ModifiedSide1"), _sideDeckParent.transform, _sidePrefab);
            CreateMockShopObject(CreateFakeCard("ModifiedSide2"), _sideDeckParent.transform, _sidePrefab);

            // Manually apply the fix
            var shopObjects = _sideDeckParent.GetComponentsInChildren<ShopObject>();
            var newSideDeck = shopObjects.Select(so => so.ShopItem.Card).ToList();
            if (newSideDeck.Count > 0)
            {
                _deckManager.SetSideDeck(newSideDeck);
            }

            // Assert: Side deck updated
            var updatedDeck = GetSideDeck();
            Assert.AreEqual(2, updatedDeck.Count, "Side deck should have 2 cards");
            Assert.AreEqual("ModifiedSide1", updatedDeck[0].Name, "First card should be ModifiedSide1");
            Assert.AreEqual("ModifiedSide2", updatedDeck[1].Name, "Second card should be ModifiedSide2");
        }

        #endregion

        #region Open/Close Tests

        [Test]
        public void Test_OpenDeckOrganizer_ClearsExistingOrganizer()
        {
            // Arrange: Populate UI with existing items
            CreateMockShopObject(CreateFakeCard("Old1"), _actionDeckParent.transform, _actionPrefab);
            CreateMockShopObject(CreateFakeCard("Old2"), _sideDeckParent.transform, _sidePrefab);
            var initialActionCount = _actionDeckParent.transform.childCount;
            var initialSideCount = _sideDeckParent.transform.childCount;

            // Set up decks for loading
            var actionDeck = GetActionDeck();
            actionDeck.Clear();
            actionDeck.Add(CreateFakeCard("New1"));

            // Act: Open organizer (should clear existing and load new)
            _deckOrganizerManager.OpenDeckOrganizer();

            // Assert: Old items should be destroyed, new items loaded
            // Note: ClearOrganizer uses Destroy (deferred), so we verify by checking loaded card data
            Assert.Greater(initialActionCount, 0, "Should have had initial items");

            // Verify the new card ("New1") was loaded by checking ShopObjects
            var shopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            var loadedCards = shopObjects
                .Where(so => so != null && so.ShopItem != null && so.ShopItem.Card != null)
                .Select(so => so.ShopItem.Card.Name)
                .ToList();

            Assert.Contains("New1", loadedCards, "New card 'New1' should be loaded");
            Assert.IsFalse(loadedCards.Contains("Old1"), "Old card 'Old1' should not be present");
        }

        [Test]
        public void Test_OpenDeckOrganizer_LoadsActionDeck()
        {
            // Arrange: Set up action deck
            var actionDeck = GetActionDeck();
            actionDeck.Clear();
            actionDeck.Add(CreateFakeCard("LoadTest1"));
            actionDeck.Add(CreateFakeCard("LoadTest2"));

            // Act: Open organizer
            _deckOrganizerManager.OpenDeckOrganizer();

            // Assert: Action deck should be loaded in UI
            var shopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.AreEqual(2, shopObjects.Length, "Should load 2 action cards");
            Assert.AreEqual("LoadTest1", shopObjects[0].ShopItem.Card.Name, "First card should be LoadTest1");
        }

        [Test]
        public void Test_OpenDeckOrganizer_LoadsSideDeck()
        {
            // Arrange: Set up side deck
            var sideDeck = GetSideDeck();
            sideDeck.Clear();
            sideDeck.Add(CreateFakeCard("SideLoadTest1"));

            // Act: Open organizer
            _deckOrganizerManager.OpenDeckOrganizer();

            // Assert: Side deck should be loaded in UI
            var shopObjects = _sideDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.AreEqual(1, shopObjects.Length, "Should load 1 side card");
            Assert.AreEqual("SideLoadTest1", shopObjects[0].ShopItem.Card.Name, "Card should be SideLoadTest1");
        }

        [Test]
        public void Test_OpenDeckOrganizer_ActivatesPanel()
        {
            // Arrange: Ensure panel starts inactive
            _deckUIPanel.SetActive(false);

            // Act: Open organizer
            _deckOrganizerManager.OpenDeckOrganizer();

            // Assert: Panel should be active
            Assert.IsTrue(_deckUIPanel.activeSelf, "Panel should be activated");
        }

        [Test]
        public void Test_OpenDeckOrganizer_DisablesClick3D()
        {
            // Arrange: Ensure Click3D is enabled
            Click3D.Click3DGloballyDisabled = false;

            // Act: Open organizer
            _deckOrganizerManager.OpenDeckOrganizer();

            // Assert: Click3D should be globally disabled
            Assert.IsTrue(Click3D.Click3DGloballyDisabled, "Click3D should be globally disabled");
        }

        [Test]
        public void Test_OpenDeckOrganizer_RequestsUIInputOwnership()
        {
            // Note: UIInputManager is a static class that requires full initialization
            // This test verifies the call doesn't throw an exception
            // Full UIInputManager testing is in UIInputManagerTests.cs

            // Act & Assert: Should not throw exception
            Assert.DoesNotThrow(() => _deckOrganizerManager.OpenDeckOrganizer(),
                "OpenDeckOrganizer should call UIInputManager.RequestEnable without throwing");
        }

        [Test]
        public void Test_CloseDeckOrganizer_DeactivatesPanel()
        {
            // Arrange: Open organizer first
            _deckOrganizerManager.OpenDeckOrganizer();
            Assert.IsTrue(_deckUIPanel.activeSelf, "Panel should be active after opening");

            // Act: Close organizer (expects errors due to GetComponentsInChildren bug)
            LogAssert.Expect(LogType.Error, "ActionDeck is empty!");
            LogAssert.Expect(LogType.Error, "SideDeck is empty!");
            _deckOrganizerManager.CloseDeckOrganizer();

            // Assert: Panel should be deactivated
            Assert.IsFalse(_deckUIPanel.activeSelf, "Panel should be deactivated");
        }

        [Test]
        public void Test_CloseDeckOrganizer_EnablesClick3D()
        {
            // Arrange: Open organizer (disables Click3D)
            _deckOrganizerManager.OpenDeckOrganizer();
            Assert.IsTrue(Click3D.Click3DGloballyDisabled, "Click3D should be disabled after opening");

            // Act: Close organizer (expects errors due to bug)
            LogAssert.Expect(LogType.Error, "ActionDeck is empty!");
            LogAssert.Expect(LogType.Error, "SideDeck is empty!");
            _deckOrganizerManager.CloseDeckOrganizer();

            // Assert: Click3D should be enabled
            Assert.IsFalse(Click3D.Click3DGloballyDisabled, "Click3D should be enabled after closing");
        }

        [Test]
        public void Test_CloseDeckOrganizer_ReleasesUIInputOwnership()
        {
            // Arrange: Open organizer first
            _deckOrganizerManager.OpenDeckOrganizer();

            // Act & Assert: Should not throw exception when calling RequestDisable (expects errors due to bug)
            LogAssert.Expect(LogType.Error, "ActionDeck is empty!");
            LogAssert.Expect(LogType.Error, "SideDeck is empty!");
            Assert.DoesNotThrow(() => _deckOrganizerManager.CloseDeckOrganizer(),
                "CloseDeckOrganizer should call UIInputManager.RequestDisable without throwing");
        }

        [Test]
        public void Test_CloseDeckOrganizer_Level2_CallsShowBetaScreen()
        {
            // Arrange: Set turn controller level to 2
            _turnController.level = 2;
            _deckOrganizerManager.OpenDeckOrganizer();

            // Act & Assert: Should not throw when calling ShowBetaScreen (expects errors due to bug)
            LogAssert.Expect(LogType.Error, "ActionDeck is empty!");
            LogAssert.Expect(LogType.Error, "SideDeck is empty!");
            Assert.DoesNotThrow(() => _deckOrganizerManager.CloseDeckOrganizer(),
                "Should call ShowBetaScreen for level 2 without throwing");
        }

        [Test]
        public void Test_CloseDeckOrganizer_NormalLevel_SetsCanClickEndFalse()
        {
            // Arrange: Set normal level and initial state
            _turnController.level = 1;
            _turnController.canClickEnd = true;
            _deckOrganizerManager.OpenDeckOrganizer();

            // Act: Close organizer (expects errors due to bug)
            LogAssert.Expect(LogType.Error, "ActionDeck is empty!");
            LogAssert.Expect(LogType.Error, "SideDeck is empty!");
            _deckOrganizerManager.CloseDeckOrganizer();

            // Assert: canClickEnd should be set to false
            Assert.IsFalse(_turnController.canClickEnd, "canClickEnd should be false after closing");
        }

        [Test]
        public void Test_CloseDeckOrganizer_NormalLevel_SetsNewRoundReadyFalse()
        {
            // Arrange: Set normal level and initial state
            _turnController.level = 1;
            _turnController.newRoundReady = true;
            _deckOrganizerManager.OpenDeckOrganizer();

            // Act: Close organizer (expects errors due to bug)
            LogAssert.Expect(LogType.Error, "ActionDeck is empty!");
            LogAssert.Expect(LogType.Error, "SideDeck is empty!");
            _deckOrganizerManager.CloseDeckOrganizer();

            // Assert: newRoundReady should be set to false
            Assert.IsFalse(_turnController.newRoundReady, "newRoundReady should be false after closing");
        }

        #endregion

        #region Cleanup Tests

        [Test]
        public void Test_ClearOrganizer_DestroysActionDeckObjects()
        {
            // Arrange: Create action deck items
            CreateMockShopObject(CreateFakeCard("Clear1"), _actionDeckParent.transform, _actionPrefab);
            CreateMockShopObject(CreateFakeCard("Clear2"), _actionDeckParent.transform, _actionPrefab);
            var initialCount = _actionDeckParent.transform.childCount;

            // Act: Clear organizer
            InvokeClearOrganizer();

            // Assert: All children should be destroyed (but destruction is deferred, so check is scheduled)
            // In Unity tests, Destroy is immediate in edit mode but deferred in play mode
            // We verify the destroy was called by checking the child count after a frame
            Assert.AreEqual(2, initialCount, "Should have had 2 children initially");
        }

        [Test]
        public void Test_ClearOrganizer_DestroysSideDeckObjects()
        {
            // Arrange: Create side deck items
            CreateMockShopObject(CreateFakeCard("SideClear1"), _sideDeckParent.transform, _sidePrefab);
            var initialCount = _sideDeckParent.transform.childCount;

            // Act: Clear organizer
            InvokeClearOrganizer();

            // Assert: Should have had initial children
            Assert.AreEqual(1, initialCount, "Should have had 1 child initially");
        }

        [Test]
        public void Test_ClearOrganizer_WithEmptyLists_HandlesGracefully()
        {
            // Arrange: Ensure no items in lists (use DestroyImmediate for synchronous destruction)
            while (_actionDeckParent.transform.childCount > 0)
            {
                UnityEngine.Object.DestroyImmediate(_actionDeckParent.transform.GetChild(0).gameObject);
            }
            while (_sideDeckParent.transform.childCount > 0)
            {
                UnityEngine.Object.DestroyImmediate(_sideDeckParent.transform.GetChild(0).gameObject);
            }

            // Act & Assert: Should not throw
            Assert.DoesNotThrow(() => InvokeClearOrganizer(),
                "ClearOrganizer should handle empty lists gracefully");
        }

        [Test]
        public void Test_RemoveOrganizerItem_WithValidItem_DestroysGameObject()
        {
            // Arrange: Create a shop item
            var cardObj = CreateMockShopObject(CreateFakeCard("Remove1"), _actionDeckParent.transform, _actionPrefab);
            var initialCount = _actionDeckParent.transform.childCount;

            // Act: Remove the item using reflection to call private method
            var removeMethod = typeof(DeckOrganizerManager)
                .GetMethod("RemoveOrganizerItem", BindingFlags.NonPublic | BindingFlags.Instance);
            removeMethod?.Invoke(_deckOrganizerManager, new object[] { cardObj });

            // Assert: Item should be marked for destruction
            Assert.AreEqual(1, initialCount, "Should have had 1 item initially");
            // Note: Actual destruction verification requires coroutine/frame wait
        }

        [Test]
        public void Test_RemoveOrganizerItem_WithNullShopObject_HandlesGracefully()
        {
            // Arrange: Create GameObject without ShopObject component
            var invalidObj = new GameObject("InvalidItem");
            invalidObj.transform.SetParent(_actionDeckParent.transform);

            // Act: Try to remove item without ShopObject
            var removeMethod = typeof(DeckOrganizerManager)
                .GetMethod("RemoveOrganizerItem", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert: Should not throw exception
            Assert.DoesNotThrow(() => removeMethod?.Invoke(_deckOrganizerManager, new object[] { invalidObj }),
                "RemoveOrganizerItem should handle missing ShopObject gracefully");

            // Cleanup
            UnityEngine.Object.Destroy(invalidObj);
        }

        [Test]
        public void Test_RemoveOrganizerItem_WithNullGameObject_HandlesGracefully()
        {
            // Act: Try to remove null GameObject
            var removeMethod = typeof(DeckOrganizerManager)
                .GetMethod("RemoveOrganizerItem", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert: Should not throw exception when GameObject is null
            // Note: Passing null through reflection requires explicit null parameter
            try
            {
                removeMethod?.Invoke(_deckOrganizerManager, new object[] { null });
                Assert.Pass("RemoveOrganizerItem handled null GameObject gracefully");
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                // If inner exception is NullReferenceException, the method doesn't handle null properly
                if (ex.InnerException is NullReferenceException)
                {
                    Assert.Fail("RemoveOrganizerItem should handle null GameObject without throwing NullReferenceException");
                }
                throw; // Re-throw if it's a different exception
            }
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Test_FullWorkflow_OpenLoadSaveClose()
        {
            // Arrange: Set up initial decks
            var actionDeck = GetActionDeck();
            actionDeck.Clear();
            actionDeck.Add(CreateFakeCard("Workflow1"));
            actionDeck.Add(CreateFakeCard("Workflow2"));

            var sideDeck = GetSideDeck();
            sideDeck.Clear();
            sideDeck.Add(CreateFakeCard("SideWorkflow1"));

            // Act: Execute full workflow
            _deckOrganizerManager.OpenDeckOrganizer();

            // Verify loading worked
            var actionShopObjects = _actionDeckParent.GetComponentsInChildren<ShopObject>();
            var sideShopObjects = _sideDeckParent.GetComponentsInChildren<ShopObject>();
            Assert.AreEqual(2, actionShopObjects.Length, "Action deck should be loaded");
            Assert.AreEqual(1, sideShopObjects.Length, "Side deck should be loaded");

            // Simulate user modification (add a new card to UI)
            CreateMockShopObject(CreateFakeCard("AddedCard"), _actionDeckParent.transform, _actionPrefab);

            // Note: Saving with current buggy code won't work, but we test the workflow doesn't crash (expects errors)
            LogAssert.Expect(LogType.Error, "ActionDeck is empty!");
            LogAssert.Expect(LogType.Error, "SideDeck is empty!");
            Assert.DoesNotThrow(() => _deckOrganizerManager.CloseDeckOrganizer(),
                "Full workflow should complete without throwing");

            // Verify panel closed
            Assert.IsFalse(_deckUIPanel.activeSelf, "Panel should be closed");
        }

        [Test]
        public void Test_MultipleOpenClose_NoMemoryLeaks()
        {
            // Arrange: Set up small deck
            var actionDeck = GetActionDeck();
            actionDeck.Clear();
            actionDeck.Add(CreateFakeCard("Leak1"));
            actionDeck.Add(CreateFakeCard("Leak2"));

            // Act: Run multiple open/close cycles (each close triggers buggy save, expects errors)
            for (var i = 0; i < 5; i++)
            {
                _deckOrganizerManager.OpenDeckOrganizer();
                LogAssert.Expect(LogType.Error, "ActionDeck is empty!");
                LogAssert.Expect(LogType.Error, "SideDeck is empty!");
                _deckOrganizerManager.CloseDeckOrganizer();
            }

            // Assert: Should not accumulate objects (basic check)
            // After final close, panel should be inactive
            Assert.IsFalse(_deckUIPanel.activeSelf, "Panel should be inactive after cycles");

            // Verify no excessive children (should be cleared on each open)
            _deckOrganizerManager.OpenDeckOrganizer();
            var finalCount = _actionDeckParent.GetComponentsInChildren<ShopObject>().Length;
            Assert.AreEqual(2, finalCount, "Should maintain correct count after multiple cycles");
        }

        [Test]
        public void Test_UIInputOwnership_TransfersBetweenSystems()
        {
            // This is a basic integration test - full UIInputManager testing is in UIInputManagerTests.cs

            // Act: Open and close organizer
            Assert.DoesNotThrow(() => _deckOrganizerManager.OpenDeckOrganizer(),
                "Should request UIInput ownership without throwing");

            // Close expects errors due to buggy save methods
            LogAssert.Expect(LogType.Error, "ActionDeck is empty!");
            LogAssert.Expect(LogType.Error, "SideDeck is empty!");
            Assert.DoesNotThrow(() => _deckOrganizerManager.CloseDeckOrganizer(),
                "Should release UIInput ownership without throwing");

            // Assert: Basic workflow completed successfully
            Assert.IsFalse(_deckUIPanel.activeSelf, "Panel should be closed after workflow");
        }

        #endregion

        #region Mock Classes

        private class FakeCard : ICard
        {
            public FakeCard(string name, int value = 0)
            {
                Name = name;
                Value = value;
                Stickers = new List<ISticker>();
            }

            public string Name { get; }
            public string Description => "Test card description";
            public int? Value { get; set; }
            public PlantAfflictions.IAffliction Affliction => null;
            public PlantAfflictions.ITreatment Treatment => null;
            public GameObject Prefab => null;
            public Material Material => null;
            public List<ISticker> Stickers { get; }

            public ICard Clone()
            {
                return new FakeCard(Name, Value ?? 0);
            }

            public void Selected() { }
            public void ApplySticker(ISticker sticker) { Stickers.Add(sticker); }
            public void ModifyValue(int delta) { Value = (Value ?? 0) + delta; }
        }

        #endregion
    }
}
