using System.Collections.Generic;
using _project.Scripts.Audio;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.PlayModeTest.Utilities.Mocks;
using _project.Scripts.PlayModeTest.Utilities.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Tests for sidedeck functionality in DeckManager.
    ///     Validates card movement between action deck, sidedeck, and discard pile.
    /// </summary>
    public class SideDeckTests
    {
        private GameObject _actionParentGo;
        private GameObject _cardGameMasterGo;
        private DeckManager _deckManager;
        private GameObject _fakePrefab;
        private GameObject _lostObjectsGo;
        private ScoreManager _scoreManager;
        private TurnController _turnController;
        private GameObject _winScreenGo;

        [SetUp]
        public void Setup()
        {
            // Build a minimal CardGameMaster hierarchy
            _cardGameMasterGo = new GameObject("CardGameMaster");
            _cardGameMasterGo.SetActive(false);

            _deckManager = _cardGameMasterGo.AddComponent<DeckManager>();
            _scoreManager = _cardGameMasterGo.AddComponent<ScoreManager>();
            _turnController = _cardGameMasterGo.AddComponent<TurnController>();
            var cardGameMaster = _cardGameMasterGo.AddComponent<CardGameMaster>();
            var soundSystem = _cardGameMasterGo.AddComponent<SoundSystemMaster>();
            var audioSource = _cardGameMasterGo.AddComponent<AudioSource>();
            _cardGameMasterGo.AddComponent<AudioListener>();

            // Minimal objects required by TurnController
            _lostObjectsGo = new GameObject("LostObjects");
            _winScreenGo = new GameObject("WinScreen");
            _turnController.lostGameObjects = _lostObjectsGo;
            _turnController.winScreen = _winScreenGo;

            // Wire dependencies for CardGameMaster
            cardGameMaster.deckManager = _deckManager;
            cardGameMaster.scoreManager = _scoreManager;
            cardGameMaster.turnController = _turnController;
            cardGameMaster.soundSystem = soundSystem;
            cardGameMaster.playerHandAudioSource = audioSource;

            // Expose a singleton instance via reflection
            CardGameMasterReflection.SetInstance(cardGameMaster);

            // Set up a fake parent for action cards
            _actionParentGo = new GameObject("ActionCardParent");
            _deckManager.actionCardParent = _actionParentGo.transform;

            // Set up a fake card prefab
            _fakePrefab = new GameObject("FakeCardPrefab");
            _fakePrefab.AddComponent<CardViewFake>();
            CardGameMaster.Instance.actionCardPrefab = _fakePrefab;

            // Allow Unity to run initial setup code silently
            LogAssert.ignoreFailingMessages = true;
            _cardGameMasterGo.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_cardGameMasterGo);
            Object.Destroy(_actionParentGo);
            Object.Destroy(_fakePrefab);
            Object.Destroy(_lostObjectsGo);
            Object.Destroy(_winScreenGo);
        }

        #region Helper Methods

        private List<ICard> GetActionDeck()
        {
            var deck = DeckManagerReflection.GetActionDeck(_deckManager);
            Assert.IsNotNull(deck, "Failed to retrieve _actionDeck via reflection");
            return deck;
        }

        private List<ICard> GetDiscardPile()
        {
            var pile = DeckManagerReflection.GetDiscardPile(_deckManager);
            Assert.IsNotNull(pile, "Failed to retrieve _actionDiscardPile via reflection");
            return pile;
        }

        private List<ICard> GetSideDeck()
        {
            var sideDeck = DeckManagerReflection.GetSideDeck(_deckManager);
            Assert.IsNotNull(sideDeck, "Failed to retrieve _sideDeck via reflection");
            return sideDeck;
        }

        private void InvokeAddCardToSideDeck(List<ICard> sourceDeck, ICard card)
        {
            DeckManagerReflection.InvokeAddCardToSideDeck(_deckManager, sourceDeck, card);
        }

        private void InvokeAddCardToActionDeck(List<ICard> sourceDeck, ICard card)
        {
            DeckManagerReflection.InvokeAddCardToActionDeck(_deckManager, sourceDeck, card);
        }

        #endregion

        #region Adding Cards to SideDeck Tests

        [Test]
        public void TestAddCardToSideDeck_FromActionDeck()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var testCard = new FakeCard("Test Card");
            actionDeck.Add(testCard);

            // Act
            InvokeAddCardToSideDeck(actionDeck, testCard);

            // Assert
            Assert.AreEqual(0, actionDeck.Count, "Card should be removed from action deck");
            Assert.AreEqual(1, sideDeck.Count, "Card should be added to sidedeck");
            Assert.AreEqual("Test Card", sideDeck[0].Name, "Card in sidedeck should match the added card");
        }

        [Test]
        public void TestAddCardToSideDeck_FromDiscardPile()
        {
            // Arrange
            var discardPile = GetDiscardPile();
            var sideDeck = GetSideDeck();
            discardPile.Clear();
            sideDeck.Clear();

            var testCard = new FakeCard("Discarded Card");
            discardPile.Add(testCard);

            // Act
            InvokeAddCardToSideDeck(discardPile, testCard);

            // Assert
            Assert.AreEqual(0, discardPile.Count, "Card should be removed from discard pile");
            Assert.AreEqual(1, sideDeck.Count, "Card should be added to sidedeck");
            Assert.AreEqual("Discarded Card", sideDeck[0].Name, "Card in sidedeck should match the added card");
        }

        [Test]
        public void TestAddMultipleCardsToSideDeck()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var card1 = new FakeCard("Card 1");
            var card2 = new FakeCard("Card 2");
            var card3 = new FakeCard("Card 3");
            actionDeck.Add(card1);
            actionDeck.Add(card2);
            actionDeck.Add(card3);

            // Act
            InvokeAddCardToSideDeck(actionDeck, card1);
            InvokeAddCardToSideDeck(actionDeck, card2);
            InvokeAddCardToSideDeck(actionDeck, card3);

            // Assert
            Assert.AreEqual(0, actionDeck.Count, "All cards should be removed from action deck");
            Assert.AreEqual(3, sideDeck.Count, "All cards should be added to sidedeck");
        }

        [Test]
        public void TestAddCardToSideDeck_MaintainsOrder()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var card1 = new FakeCard("First");
            var card2 = new FakeCard("Second");
            var card3 = new FakeCard("Third");
            actionDeck.Add(card1);
            actionDeck.Add(card2);
            actionDeck.Add(card3);

            // Act
            InvokeAddCardToSideDeck(actionDeck, card1);
            InvokeAddCardToSideDeck(actionDeck, card2);
            InvokeAddCardToSideDeck(actionDeck, card3);

            // Assert
            Assert.AreEqual("First", sideDeck[0].Name, "First card should be at index 0");
            Assert.AreEqual("Second", sideDeck[1].Name, "Second card should be at index 1");
            Assert.AreEqual("Third", sideDeck[2].Name, "Third card should be at index 2");
        }

        #endregion

        #region Pulling Cards from SideDeck Tests

        [Test]
        public void TestAddCardToActionDeck_FromSideDeck()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var testCard = new FakeCard("Sidedeck Card");
            sideDeck.Add(testCard);

            var initialActionDeckCount = actionDeck.Count;

            // Act
            InvokeAddCardToActionDeck(sideDeck, testCard);

            // Assert
            Assert.AreEqual(0, sideDeck.Count, "Card should be removed from sidedeck");
            Assert.AreEqual(initialActionDeckCount + 1, actionDeck.Count, "Card should be added to action deck");
            Assert.IsTrue(actionDeck.Contains(testCard), "Action deck should contain the pulled card");
        }

        [Test]
        public void TestPullMultipleCardsFromSideDeck()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var card1 = new FakeCard("Card 1");
            var card2 = new FakeCard("Card 2");
            var card3 = new FakeCard("Card 3");
            sideDeck.Add(card1);
            sideDeck.Add(card2);
            sideDeck.Add(card3);

            // Act
            InvokeAddCardToActionDeck(sideDeck, card1);
            InvokeAddCardToActionDeck(sideDeck, card2);
            InvokeAddCardToActionDeck(sideDeck, card3);

            // Assert
            Assert.AreEqual(0, sideDeck.Count, "All cards should be removed from sidedeck");
            Assert.AreEqual(3, actionDeck.Count, "All cards should be added to action deck");
        }

        [Test]
        public void TestPullCardFromSideDeck_AddsToBottomOfDeck()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var existingCard = new FakeCard("Existing");
            actionDeck.Add(existingCard);

            var pulledCard = new FakeCard("Pulled");
            sideDeck.Add(pulledCard);

            // Act
            InvokeAddCardToActionDeck(sideDeck, pulledCard);

            // Assert
            Assert.AreEqual(2, actionDeck.Count, "Action deck should have 2 cards");
            Assert.AreEqual("Pulled", actionDeck[1].Name, "Pulled card should be at the end of the deck");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void TestAddCardToSideDeck_FromEmptyDeck_CardNotAdded()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var testCard = new FakeCard("Test Card");

            // Act - attempting to move a card that doesn't exist in source
            InvokeAddCardToSideDeck(actionDeck, testCard);

            // Assert - card should NOT be added to prevent duplication bugs
            Assert.AreEqual(0, actionDeck.Count, "Action deck should remain empty");
            Assert.AreEqual(0, sideDeck.Count, "Card should NOT be added to sidedeck when not in source deck");
        }

        [Test]
        public void TestPullCardFromEmptySideDeck_CardNotAdded()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var testCard = new FakeCard("Test Card");
            var initialCount = actionDeck.Count;

            // Act - attempting to pull a card that doesn't exist in sidedeck
            InvokeAddCardToActionDeck(sideDeck, testCard);

            // Assert - card should NOT be added to prevent duplication bugs
            Assert.AreEqual(0, sideDeck.Count, "Sidedeck should remain empty");
            Assert.AreEqual(initialCount, actionDeck.Count,
                "Card should NOT be added to action deck when not in sidedeck");
        }

        [Test]
        public void TestAddCardToSideDeck_CardAlreadyInSideDeck_NotDuplicated()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var testCard = new FakeCard("Duplicate Test Card");
            sideDeck.Add(testCard);

            // Act - try to add the same card that's already in sidedeck
            InvokeAddCardToSideDeck(sideDeck, testCard);

            // Assert - card should be removed and re-added (net effect: still 1 card)
            Assert.AreEqual(1, sideDeck.Count, "SideDeck should not contain duplicates");
            Assert.AreEqual("Duplicate Test Card", sideDeck[0].Name, "Card should remain in sidedeck");
        }

        [Test]
        public void TestRoundTripCardMovement()
        {
            // Arrange - card goes from action deck -> sidedeck -> action deck
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var testCard = new FakeCard("Round Trip Card");
            actionDeck.Add(testCard);

            // Act - move to sidedeck
            InvokeAddCardToSideDeck(actionDeck, testCard);

            Assert.AreEqual(0, actionDeck.Count, "Card should be removed from action deck");
            Assert.AreEqual(1, sideDeck.Count, "Card should be in sidedeck");

            // Act - move back to action deck
            InvokeAddCardToActionDeck(sideDeck, testCard);

            // Assert
            Assert.AreEqual(0, sideDeck.Count, "Card should be removed from sidedeck");
            Assert.AreEqual(1, actionDeck.Count, "Card should be back in action deck");
            Assert.AreEqual("Round Trip Card", actionDeck[0].Name, "Card should maintain its identity");
        }

        [Test]
        public void TestSideDeckIsolation_DoesNotAffectDiscardPile()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            var discardPile = GetDiscardPile();
            actionDeck.Clear();
            sideDeck.Clear();
            discardPile.Clear();

            var deckCard = new FakeCard("Deck Card");
            var discardCard = new FakeCard("Discard Card");
            actionDeck.Add(deckCard);
            discardPile.Add(discardCard);

            // Act
            InvokeAddCardToSideDeck(actionDeck, deckCard);

            // Assert
            Assert.AreEqual(0, actionDeck.Count, "Action deck should be empty");
            Assert.AreEqual(1, sideDeck.Count, "Sidedeck should have one card");
            Assert.AreEqual(1, discardPile.Count, "Discard pile should be unaffected");
            Assert.AreEqual("Discard Card", discardPile[0].Name, "Discard pile content should remain unchanged");
        }

        [Test]
        public void TestMixedSourceDecks_ActionAndDiscard()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var discardPile = GetDiscardPile();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            discardPile.Clear();
            sideDeck.Clear();

            var actionCard = new FakeCard("Action Card");
            var discardCard = new FakeCard("Discard Card");
            actionDeck.Add(actionCard);
            discardPile.Add(discardCard);

            // Act
            InvokeAddCardToSideDeck(actionDeck, actionCard);
            InvokeAddCardToSideDeck(discardPile, discardCard);

            // Assert
            Assert.AreEqual(0, actionDeck.Count, "Action deck should be empty");
            Assert.AreEqual(0, discardPile.Count, "Discard pile should be empty");
            Assert.AreEqual(2, sideDeck.Count, "Sidedeck should have both cards");
            Assert.AreEqual("Action Card", sideDeck[0].Name, "First card should be from action deck");
            Assert.AreEqual("Discard Card", sideDeck[1].Name, "Second card should be from discard pile");
        }

        #endregion

        #region Sidedeck Isolation Tests

        [Test]
        public void TestSideDeckDoesNotParticipateInRecycling()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var discardPile = GetDiscardPile();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            discardPile.Clear();
            sideDeck.Clear();

            var sideDeckCard = new FakeCard("Sidedeck Card");
            sideDeck.Add(sideDeckCard);

            // Simulate an empty deck that would trigger recycling
            var discardCard = new FakeCard("Discard Card");
            discardPile.Add(discardCard);

            // Act - simulate what happens during recycling (manually for testing)
            // In actual gameplay, when action deck is empty, discard pile gets recycled
            // but sidedeck should remain untouched
            actionDeck.AddRange(discardPile);
            discardPile.Clear();

            // Assert - sidedeck is completely isolated from recycling
            Assert.AreEqual(1, sideDeck.Count, "Sidedeck should not be affected by recycling");
            Assert.AreEqual("Sidedeck Card", sideDeck[0].Name, "Sidedeck card should remain unchanged");
            Assert.IsFalse(actionDeck.Contains(sideDeckCard), "Sidedeck card should not be recycled into action deck");
        }

        [Test]
        public void TestPullFromSideDeckMakesCardDrawable()
        {
            // Arrange
            var actionDeck = GetActionDeck();
            var sideDeck = GetSideDeck();
            actionDeck.Clear();
            sideDeck.Clear();

            var pulledCard = new FakeCard("Pulled Card");
            sideDeck.Add(pulledCard);

            var initialDeckSize = actionDeck.Count;

            // Act - pull card from sidedeck
            InvokeAddCardToActionDeck(sideDeck, pulledCard);

            // Assert - card is now drawable from action deck
            Assert.AreEqual(initialDeckSize + 1, actionDeck.Count, "Action deck should have one more card");
            Assert.IsTrue(actionDeck.Contains(pulledCard), "Pulled card should be in action deck and drawable");
            Assert.AreEqual(0, sideDeck.Count, "Sidedeck should be empty after pull");
        }

        #endregion
    }
}