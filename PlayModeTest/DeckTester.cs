using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Audio;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable PossibleNullReferenceException

namespace _project.Scripts.PlayModeTest
{
    public class DeckManagerTests
    {
        private GameObject _actionParentGo;
        private GameObject _cardGameMasterGo;

        private DeckManager deckManager;
        private ScoreManager _scoreManager;
        private TurnController _turnController;
        private GameObject fakePrefab;
        private GameObject _lostObjectsGo;
        private GameObject _winScreenGo;
        
        // Fake implementation of ICard for testing.
        private class FakeCard : ICard
        {
            public FakeCard(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public ICard Clone()
            {
                return new FakeCard(Name);
            }
        }

        private class CardViewFake : MonoBehaviour { }

        [SetUp]
        public void Setup()
        {
            // Build a minimal CardGameMaster hierarchy.
            _cardGameMasterGo = new GameObject("CardGameMaster");
            _cardGameMasterGo.SetActive(false);

            deckManager = _cardGameMasterGo.AddComponent<DeckManager>();
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

            // Wire dependencies for CardGameMaster.
            cardGameMaster.deckManager = deckManager;
            cardGameMaster.scoreManager = _scoreManager;
            cardGameMaster.turnController = _turnController;
            cardGameMaster.soundSystem = soundSystem;
            cardGameMaster.playerHandAudioSource = audioSource;

            // Expose singleton instance via reflection.
            typeof(CardGameMaster)
                .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?.SetValue(null, cardGameMaster);

            // Set up a fake parent for action cards.
            _actionParentGo = new GameObject("ActionCardParent");
            deckManager.actionCardParent = _actionParentGo.transform;

            // Set up a fake card prefab with CardViewFake.
            fakePrefab = new GameObject("FakeCardPrefab");
            fakePrefab.AddComponent<CardViewFake>();
            deckManager.cardPrefab = fakePrefab;

            // Allow Unity to run initial setup code silently.
            LogAssert.ignoreFailingMessages = true;
            _cardGameMasterGo.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_cardGameMasterGo);
            Object.Destroy(_actionParentGo);
            Object.Destroy(fakePrefab);
            Object.Destroy(_lostObjectsGo);
            Object.Destroy(_winScreenGo);
        }

        #region Regular DrawActionHand Test

        [UnityTest]
        public IEnumerator TestDrawActionHand()
        {
            // Set up the action deck with some cards.
            var actionDeckField =
                typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();
            
            const int numOfFakeCards = 5;
            for (var i = 0; i < numOfFakeCards; i++)
            {
                actionDeck.Add(new FakeCard("Fake " + i));
            }

            // Ensure _actionHand and _actionDiscardPile are empty.
            var actionHandField =
                typeof(DeckManager).GetField("_actionHand", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDiscardField =
                typeof(DeckManager).GetField("_actionDiscardPile", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionHand = actionHandField.GetValue(deckManager) as List<ICard>;
            var actionDiscard = actionDiscardField.GetValue(deckManager) as List<ICard>;
            actionHand.Clear();
            actionDiscard.Clear();

            // Call DrawActionHand to draw cards.
            deckManager.DrawActionHand();

            // Wait for the coroutine to finish.
            yield return new WaitForSeconds(deckManager.cardsDrawnPerTurn * 0.5f + 0.1f);

            // Assert that the action hand contains the expected number of cards.
            Assert.AreEqual(deckManager.cardsDrawnPerTurn, actionHand.Count,
                $"Expected {deckManager.cardsDrawnPerTurn} cards in the action hand, but found {actionHand.Count}.");
        }

        #endregion

        #region Edge Case Tests

        [UnityTest]
        public IEnumerator TestEmptyActionDeck()
        {
            // Get the current value of cardsDrawnPerTurn (this is the number of cards we want to draw).
            var cardsToDraw = deckManager.cardsDrawnPerTurn;

            // Set up the action deck to be empty.
            var actionDeckField =
                typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();

            // Set up the discard pile with some cards.
            var actionDiscardField =
                typeof(DeckManager).GetField("_actionDiscardPile", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDiscard = actionDiscardField.GetValue(deckManager) as List<ICard>;
            actionDiscard.Clear();

            // Add cards to the discard pile (to simulate recycling).
            for (var i = 0; i < deckManager.cardsDrawnPerTurn; i++) actionDiscard.Add(new FakeCard($"Fake {i + 1}"));

            // Call DrawActionHand (it should recycle the discard pile if needed).
            deckManager.DrawActionHand();

            // Wait for the coroutine to finish.
            yield return new WaitForSeconds(cardsToDraw * 0.5f + 0.1f); // Wait dynamically based on cardsToDraw.

            // Check the action hand after recycling.
            var actionHandField =
                typeof(DeckManager).GetField("_actionHand", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionHand = actionHandField.GetValue(deckManager) as List<ICard>;

            // Ensure that the hand contains the expected number of cards (should match cardsToDraw).
            Assert.AreEqual(cardsToDraw, actionHand.Count,
                $"Expected action hand to contain {cardsToDraw} cards after recycling.");
        }


        [UnityTest]
        public IEnumerator TestEmptyDiscardPile()
        {
            // Clear both action deck and discard pile.
            var actionDeckField =
                typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();
            var actionDiscardField =
                typeof(DeckManager).GetField("_actionDiscardPile", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDiscard = actionDiscardField.GetValue(deckManager) as List<ICard>;
            actionDiscard.Clear();

            // Call DrawActionHand and check if it handles empty decks properly.
            deckManager.DrawActionHand();

            // Wait for the coroutine to finish.
            yield return new WaitForSeconds(deckManager.cardsDrawnPerTurn * 0.5f + 0.1f);

            // Assert that no cards are drawn when there is no discard pile.
            var actionHandField =
                typeof(DeckManager).GetField("_actionHand", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionHand = actionHandField.GetValue(deckManager) as List<ICard>;
            Assert.AreEqual(0, actionHand.Count, "Action hand should be empty when both decks are empty.");
        }

        [UnityTest]
        public IEnumerator TestRecyclingWhenActionDeckIsEmptyOrTooSmall()
        {
            // Get the current value of cardsDrawnPerTurn (this is the number of cards we want to draw).
            var cardsToDraw = deckManager.cardsDrawnPerTurn;

            // Set up the action deck to be empty (simulate the deck being exhausted).
            var actionDeckField =
                typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();

            // Set up the discard pile
            var actionDiscardField =
                typeof(DeckManager).GetField("_actionDiscardPile", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDiscard = actionDiscardField.GetValue(deckManager) as List<ICard>;
            actionDiscard.Clear();

            // Add enough cards to the discard pile to meet the draw count.
            for (var i = 0; i < cardsToDraw; i++) actionDiscard.Add(new FakeCard($"Discarded {i + 1}"));

            // Call DrawActionHand (it should recycle the discard pile if needed).
            deckManager.DrawActionHand();

            // Wait for the coroutine to finish.
            yield return new WaitForSeconds(cardsToDraw * 0.5f + 0.1f); // Wait dynamically based on cardsToDraw.

            // Check the action hand after recycling.
            var actionHandField =
                typeof(DeckManager).GetField("_actionHand", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionHand = actionHandField.GetValue(deckManager) as List<ICard>;

            // Ensure that the hand contains the expected number of cards (should match cardsToDraw).
            Assert.AreEqual(cardsToDraw, actionHand.Count,
                $"Expected action hand to contain {cardsToDraw} cards after recycling.");
        }


        [UnityTest]
        public IEnumerator TestActionDeckExactlyMatchesDrawCount()
        {
            // Set up the action deck to exactly match the draw count.
            var actionDeckField =
                typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();
            // Draw as many cards as cardsDrawnPerTurn.
            for (var i = 0; i < deckManager.cardsDrawnPerTurn; i++) actionDeck.Add(new FakeCard($"Fake {i + 1}"));
            // Ensure _actionHand and _actionDiscardPile are empty.
            var actionHandField =
                typeof(DeckManager).GetField("_actionHand", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDiscardField =
                typeof(DeckManager).GetField("_actionDiscardPile", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionHand = actionHandField.GetValue(deckManager) as List<ICard>;
            var actionDiscard = actionDiscardField.GetValue(deckManager) as List<ICard>;
            actionHand.Clear();
            actionDiscard.Clear();

            // Call DrawActionHand to draw cards.
            deckManager.DrawActionHand();

            // Wait for the coroutine to finish.
            yield return new WaitForSeconds(deckManager.cardsDrawnPerTurn * 0.5f + 0.1f);

            // Assert that the action hand contains the expected number of cards (which should match the draw count).
            Assert.AreEqual(deckManager.cardsDrawnPerTurn, actionHand.Count,
                $"Expected {deckManager.cardsDrawnPerTurn} cards in the action hand, but found {actionHand.Count}.");
        }

        #endregion
    }
}
