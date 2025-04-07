using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace _project.Scripts.PlayModeTest
{
    public class TurnTester
    {
        private GameObject _cardGameMasterGo;
        private GameObject _plantSpawnGo;
        private DeckManager deckManager;
        private ScoreManager scoreManager;
        private TurnController turnController;

        // Dummy implementation for treatment.
        private class DummyTreatment : PlantAfflictions.ITreatment
        {
            public string Name => "Panacea";
            public string Description => "Cures all afflictions";
            public int BeeValue => 0;

            public void ApplyTreatment(PlantController plant)
            {
                var afflictions = plant.CurrentAfflictions != null
                    ? new List<PlantAfflictions.IAffliction>(plant.CurrentAfflictions)
                    : new List<PlantAfflictions.IAffliction>();
                foreach (var affliction in afflictions)
                {
                    plant.RemoveAffliction(affliction);
                }
            }
        }

        // Dummy affliction for testing.
        private class DummyAffliction : PlantAfflictions.IAffliction
        {
            public string Name => "Test Affliction";
            public string Description => "Just a test";
            public int Damage => 2;
            public Color Color => Color.gray;

            public void TreatWith(PlantAfflictions.ITreatment treatment, PlantController plant)
            {
                plant.RemoveAffliction(this);
            }

            public void TickDay() { }
        }

        // Dummy card that carries a treatment.
        private class DummyCard : ICard
        {
            public DummyCard(string name, PlantAfflictions.ITreatment treatment)
            {
                Name = name;
                Treatment = treatment;
            }

            public string Name { get; }
            public PlantAfflictions.ITreatment Treatment { get; }
            public string Description => "Test card";
            public ICard Clone() => new DummyCard(Name, Treatment);
        }

        // Fake MonoBehaviour to bypass Click3D
        // Safe subclass of Click3D that disables Start logic.
        private class SafeClick3D : Click3D
        {
            private void Start() { /* No-op to prevent scene check from running */ }
        }


        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Create the CardGameMaster GameObject inactive to prevent early Awake.
            _cardGameMasterGo = new GameObject("CardGameMaster");
            _cardGameMasterGo.SetActive(false);

            // Add required components.
            deckManager = _cardGameMasterGo.AddComponent<DeckManager>();
            scoreManager = _cardGameMasterGo.AddComponent<ScoreManager>();
            var cardGameMaster = _cardGameMasterGo.AddComponent<CardGameMaster>();
            turnController = _cardGameMasterGo.AddComponent<TurnController>();

            // Inject dependencies into CardGameMaster.
            cardGameMaster.deckManager = deckManager;
            cardGameMaster.scoreManager = scoreManager;
            cardGameMaster.turnController = turnController;

            // Use reflection to set the private static Instance property.
            typeof(CardGameMaster)
                .GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)
                ?.SetValue(null, cardGameMaster);

            // Activate the GameObject so that Awake is called.
            _cardGameMasterGo.SetActive(true);
            yield return null; // Wait a frame so Awake runs.

            // --- Set up the plant & card holder hierarchy ---
            _plantSpawnGo = new GameObject("PlantSpawn");

            // Create a plant with a PlantController.
            var plantGo = new GameObject("Plant");
            plantGo.transform.SetParent(_plantSpawnGo.transform);
            var plant = plantGo.AddComponent<PlantController>();
            plant.PlantCard = new ColeusCard();
            plant.CurrentAfflictions.Add(new DummyAffliction());

            var plantFunctions = plantGo.AddComponent<PlantCardFunctions>();
            plantFunctions.plantController = plant;
            plantFunctions.deckManager = deckManager;
            plant.plantCardFunctions = plantFunctions;

            // Create a card holder.
            var cardHolderGo = new GameObject("CardHolder");
            cardHolderGo.transform.SetParent(_plantSpawnGo.transform);
            var cardHolder = cardHolderGo.AddComponent<PlacedCardHolder>();

            // Add the card holder to CardGameMaster.Instance.cardHolders.
            var cardHoldersField = typeof(CardGameMaster)
                .GetField("cardHolders", BindingFlags.Instance | BindingFlags.Public);
            List<PlacedCardHolder> holders = cardHoldersField.GetValue(cardGameMaster) as List<PlacedCardHolder>;
            if (holders == null)
            {
                holders = new List<PlacedCardHolder>();
                cardHoldersField.SetValue(cardGameMaster, holders);
            }
            holders.Add(cardHolder);

            // Create a card with a CardView.
            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(cardHolderGo.transform);
            var cardView = cardGo.AddComponent<CardView>();

            // Add a FakeClick3D to bypass Click3D.Start logic.
            var fakeClick = cardGo.AddComponent<SafeClick3D>();
            typeof(PlacedCardHolder)
                .GetField("placedCardClick3D", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(cardHolder, fakeClick);

            // Use a DummyCard carrying our DummyTreatment.
            var dummyCard = new DummyCard("Healing Card", new DummyTreatment());
            typeof(CardView)
                .GetField("_originalCard", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(cardView, dummyCard);

            cardHolder.placedCardView = cardView;
            cardHolder.PlacedCard = dummyCard;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_cardGameMasterGo);
            Object.Destroy(_plantSpawnGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyQueuedTreatments_RemovesAffliction()
        {
            var plant = _plantSpawnGo.GetComponentInChildren<PlantController>();
            Assert.AreEqual(1, plant.CurrentAfflictions.Count, "Plant should start with 1 affliction.");

            // Call the method to apply queued treatments.
            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(0, plant.CurrentAfflictions.Count, "Affliction should have been removed.");
        }
    }
}
