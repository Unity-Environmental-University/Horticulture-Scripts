using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace _project.Scripts.PlayModeTest
{
    public class DeckManagerTests
    {
        // Dummy implementation of ICard for testing.
        private class DummyCard : ICard
        {
            public string Name { get; private set; }

            public DummyCard(string name)
            {
                Name = name;
            }

            public ICard Clone()
            {
                return new DummyCard(Name);
            }
        }

        private class CardViewDummy : MonoBehaviour
        {
            public void Setup(ICard card) { }
        }

        private DeckManager deckManager;
        private GameObject _deckManagerGo;
        private GameObject _actionParentGo;
        private GameObject dummyPrefab;

        [SetUp]
        public void Setup()
        {
            // Create the DeckManager test setup.
            _deckManagerGo = new GameObject("DeckManagerTestObject");
            deckManager = _deckManagerGo.AddComponent<DeckManager>();

            // Set up a dummy parent for action cards.
            _actionParentGo = new GameObject("ActionCardParent");
            deckManager.actionCardParent = _actionParentGo.transform;

            // Set up a dummy card prefab with CardViewDummy.
            dummyPrefab = new GameObject("DummyCardPrefab");
            dummyPrefab.AddComponent<CardViewDummy>();
            deckManager.cardPrefab = dummyPrefab;
            
            // Allow Unity to run initial setup code.
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_deckManagerGo);
            Object.Destroy(_actionParentGo);
            Object.Destroy(dummyPrefab);
        }

        #region Regular DrawActionHand Test

        [UnityTest]
        public IEnumerator TestDrawActionHand()
        {
            // Set up the action deck with some cards.
            var actionDeckField = typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();
            actionDeck.Add(new DummyCard("Dummy 1"));
            actionDeck.Add(new DummyCard("Dummy 2"));
            actionDeck.Add(new DummyCard("Dummy 3"));
            actionDeck.Add(new DummyCard("Dummy 4"));
            actionDeck.Add(new DummyCard("Dummy 5"));

            // Ensure _actionHand and _actionDiscardPile are empty.
            var actionHandField = typeof(DeckManager).GetField("_actionHand", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDiscardField = typeof(DeckManager).GetField("_actionDiscardPile", BindingFlags.NonPublic | BindingFlags.Instance);
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
            // Ensure the action deck is empty.
            var actionDeckField = typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();

            // Call DrawActionHand with an empty deck.
            deckManager.DrawActionHand();

            // Wait for coroutine to finish.
            yield return new WaitForSeconds(deckManager.cardsDrawnPerTurn * 0.5f + 0.1f);

            // Assert that the action hand contains the expected number of cards after recycling.
            var actionHandField = typeof(DeckManager).GetField("_actionHand", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionHand = actionHandField.GetValue(deckManager) as List<ICard>;
            Assert.AreEqual(deckManager.cardsDrawnPerTurn, actionHand.Count, "Expected action hand to contain drawn cards after recycling.");
        }

        [UnityTest]
        public IEnumerator TestEmptyDiscardPile()
        {
            // Clear both action deck and discard pile.
            var actionDeckField = typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();
            var actionDiscardField = typeof(DeckManager).GetField("_actionDiscardPile", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDiscard = actionDiscardField.GetValue(deckManager) as List<ICard>;
            actionDiscard.Clear();

            // Call DrawActionHand and check if it handles empty decks properly.
            deckManager.DrawActionHand();

            // Wait for the coroutine to finish.
            yield return new WaitForSeconds(deckManager.cardsDrawnPerTurn * 0.5f + 0.1f);

            // Assert that no cards are drawn when there is no discard pile.
            var actionHandField = typeof(DeckManager).GetField("_actionHand", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionHand = actionHandField.GetValue(deckManager) as List<ICard>;
            Assert.AreEqual(0, actionHand.Count, "Action hand should be empty when both decks are empty.");
        }

        [UnityTest]
        public IEnumerator TestActionDeckSmallerThanDrawCount()
        {
            // Get the current value of cardsDrawnPerTurn (this is the number of cards we want to draw).
            var cardsToDraw = deckManager.cardsDrawnPerTurn;

            // Set up the action deck to contain fewer cards than the cardsToDraw.
            var actionDeckField =
                typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();

            // Add a number of cards less than cardsToDraw to simulate an underfilled deck.
            // In this case, add 2 cards if cardsToDraw is 3.
            var cardsInDeck = Mathf.Min(2, cardsToDraw - 1); // Always add one less than cardsToDraw for the test case
            for (var i = 0; i < cardsInDeck; i++) actionDeck.Add(new DummyCard($"Dummy {i + 1}"));

            // Set up the discard pile with some cards (if needed, this can simulate recycling).
            var actionDiscardField =
                typeof(DeckManager).GetField("_actionDiscardPile", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDiscard = actionDiscardField.GetValue(deckManager) as List<ICard>;
            actionDiscard.Clear();
            for (var i = 0; i < deckManager.cardsDrawnPerTurn; i++) actionDiscard.Add(new DummyCard($"Discarded {i + 1}"));

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
            var actionDeckField = typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();
            // Draw as many cards as cardsDrawnPerTurn.
            for (var i = 0; i < deckManager.cardsDrawnPerTurn; i++)
            {
                actionDeck.Add(new DummyCard($"Dummy {i + 1}"));
            }
            // Ensure _actionHand and _actionDiscardPile are empty.
            var actionHandField = typeof(DeckManager).GetField("_actionHand", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDiscardField = typeof(DeckManager).GetField("_actionDiscardPile", BindingFlags.NonPublic | BindingFlags.Instance);
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

        [UnityTest]
        public IEnumerator TestRecycledDiscardPile()
        {
            // Set up the action deck and discard pile.
            var actionDeckField = typeof(DeckManager).GetField("_actionDeck", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDeck = actionDeckField.GetValue(deckManager) as List<ICard>;
            actionDeck.Clear();
            actionDeck.Add(new DummyCard("Dummy 1"));

            var actionDiscardField = typeof(DeckManager).GetField("_actionDiscardPile", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionDiscard = actionDiscardField.GetValue(deckManager) as List<ICard>;
            actionDiscard.Clear();
            actionDiscard.Add(new DummyCard("Discarded 1"));

            // Call DrawActionHand.
            deckManager.DrawActionHand();

            // Wait for the coroutine to finish.
            yield return new WaitForSeconds(deckManager.cardsDrawnPerTurn * 0.5f + 0.1f);

            // Check if discard pile was recycled.
            var actionHandField = typeof(DeckManager).GetField("_actionHand", BindingFlags.NonPublic | BindingFlags.Instance);
            var actionHand = actionHandField.GetValue(deckManager) as List<ICard>;
            Assert.AreEqual(deckManager.cardsDrawnPerTurn, actionHand.Count, "Expected action hand to contain drawn cards after recycling the discard pile.");
        }

        #endregion
    }
}
