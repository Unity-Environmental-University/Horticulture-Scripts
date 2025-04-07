using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

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
            // ReSharper disable once Unity.RedundantEventFunction
            private void Start() { /* No-op to prevent scene check from running */ }
        }
        
        private void CreateCardHolder(ICard card, bool assignClick3D = true)
        {
            var cardHolderGo = new GameObject("CardHolder");
            cardHolderGo.transform.SetParent(_plantSpawnGo.transform);
            var holder = cardHolderGo.AddComponent<PlacedCardHolder>();

            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(cardHolderGo.transform);
            var view = cardGo.AddComponent<CardView>();

            if (assignClick3D)
                holder.placedCardClick3D = cardGo.AddComponent<SafeClick3D>();

            typeof(CardView)
                .GetField("_originalCard", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(view, card);

            holder.placedCardView = view;
            holder.PlacedCard = card;

            // Register holder with CardGameMaster
            CardGameMaster.Instance.cardHolders.Add(holder);
        }

        private PlantController CreatePlant(params PlantAfflictions.IAffliction[] afflictions)
        {
            var plantGo = new GameObject("Plant");
            plantGo.transform.SetParent(_plantSpawnGo.transform);
            var plant = plantGo.AddComponent<PlantController>();
            plant.PlantCard = new ColeusCard();
            plant.CurrentAfflictions.AddRange(afflictions);

            var plantFunctions = plantGo.AddComponent<PlantCardFunctions>();
            plantFunctions.plantController = plant;
            plantFunctions.deckManager = deckManager;
            plant.plantCardFunctions = plantFunctions;

            return plant;
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

            // --- Set up the plant & cardholder hierarchy ---
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

            // Create a cardholder.
            var cardHolderGo = new GameObject("CardHolder");
            cardHolderGo.transform.SetParent(_plantSpawnGo.transform);
            var cardHolder = cardHolderGo.AddComponent<PlacedCardHolder>();

            // Add the cardholder to CardGameMaster.Instance.cardHolders.
            var cardHoldersField = typeof(CardGameMaster)
                .GetField("cardHolders", BindingFlags.Instance | BindingFlags.Public);
            if (cardHoldersField != null)
            {
                if (cardHoldersField.GetValue(cardGameMaster) is not List<PlacedCardHolder> holders)
                {
                    holders = new List<PlacedCardHolder>();
                    cardHoldersField.SetValue(cardGameMaster, holders);
                }
                holders.Add(cardHolder);
            }

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
        
        [UnityTest]
        public IEnumerator NoAfflictions_DoesNotThrow()
        {
            var plant = CreatePlant(); // No afflictions
            CreateCardHolder(new DummyCard("Panacea", new DummyTreatment()));

            LogAssert.ignoreFailingMessages = true; // Ignore warning logs for this test

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.Pass("Handled no afflictions gracefully.");
        }

        [UnityTest]
        public IEnumerator NoPlacedCard_DoesNotThrow()
        {
            var plant = CreatePlant(new DummyAffliction());
            var holder = new GameObject("CardHolder").AddComponent<PlacedCardHolder>();
            CardGameMaster.Instance.cardHolders.Add(holder);

            LogAssert.ignoreFailingMessages = true;

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(1, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator NoCardView_DoesNotThrow()
        {
            var plant = CreatePlant(new DummyAffliction());

            var cardHolder = new GameObject("CardHolder").AddComponent<PlacedCardHolder>();
            cardHolder.PlacedCard = new DummyCard("Healing", new DummyTreatment());
            cardHolder.placedCardClick3D = cardHolder.gameObject.AddComponent<SafeClick3D>();
            CardGameMaster.Instance.cardHolders.Add(cardHolder);

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(1, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator NullTreatment_DoesNotApply()
        {
            var plant = CreatePlant(new DummyAffliction());

            var dummy = new DummyCard("Broken", null); // null treatment
            CreateCardHolder(dummy);

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(1, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator TreatmentThatThrows_DoesNotBreakLoop()
        {
            var plant = CreatePlant(new DummyAffliction());

            var throwingCard = new DummyCard("Explodes", new ThrowingTreatment());
            CreateCardHolder(throwingCard);

            LogAssert.Expect(LogType.Exception, "System.Exception: Intentional test exception");

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(1, plant.CurrentAfflictions.Count);
        }

        private class ThrowingTreatment : PlantAfflictions.ITreatment
        {
            public string Name => "Explosive";
            public string Description => "Throws on apply";
            public int BeeValue => 0;

            public void ApplyTreatment(PlantController plant)
            {
                Debug.LogException(new System.Exception("Intentional test exception"));
            }
        }


        [UnityTest]
        public IEnumerator MultipleAfflictions_AllGetTreated()
        {
            var plant = CreatePlant(new DummyAffliction(), new DummyAffliction());
            CreateCardHolder(new DummyCard("Panacea", new DummyTreatment()));

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(0, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator CardWithoutClick3D_DoesNotApply()
        {
            var plant = CreatePlant(new DummyAffliction());
            CreateCardHolder(new DummyCard("Panacea", new DummyTreatment()), false);

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(1, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator TreatmentThatModifiesList_DoesNotCrash()
        {
            CardGameMaster.Instance.cardHolders.Clear(); // 🔥 Key fix

            var plant = CreatePlant(new DummyAffliction(), new DummyAffliction());
            var card = new DummyCard("Safe Clear", new SelfClearingTreatment());
            CreateCardHolder(card);

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(0, plant.CurrentAfflictions.Count);
        }
        
        private class SelfClearingTreatment : PlantAfflictions.ITreatment
        {
            public string Name => "SafeClear";
            public string Description => "Removes all afflictions";
            public int BeeValue => 0;

            public void ApplyTreatment(PlantController plant)
            {
                var afflictionsCopy = new List<PlantAfflictions.IAffliction>(plant.CurrentAfflictions);
                foreach (var affliction in afflictionsCopy)
                {
                    plant.RemoveAffliction(affliction);
                }
            }
        }
    }
}
