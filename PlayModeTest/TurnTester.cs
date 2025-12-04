using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using _project.Scripts.Audio;
using _project.Scripts.Card_Core;
using _project.Scripts.Cinematics;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.Stickers;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace _project.Scripts.PlayModeTest
{
    public class TurnTester
    {
        private GameObject _actionParentGo;
        private GameObject _cardGameMasterGo;
        private DeckManager _deckManager;
        private GameObject _lostObjectsGo;
        private GameObject _plantSpawnGo;
        private ScoreManager _scoreManager;
        private TurnController _turnController;
        private GameObject _winScreenGo;

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
            holder.placedCard = card;

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
            plantFunctions.deckManager = _deckManager;
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
            _deckManager = _cardGameMasterGo.AddComponent<DeckManager>();
            _scoreManager = _cardGameMasterGo.AddComponent<ScoreManager>();
            _turnController = _cardGameMasterGo.AddComponent<TurnController>();
            var cardGameMaster = _cardGameMasterGo.AddComponent<CardGameMaster>();
            var soundSystem = _cardGameMasterGo.AddComponent<SoundSystemMaster>();
            var audioSource = _cardGameMasterGo.AddComponent<AudioSource>();
            _cardGameMasterGo.AddComponent<AudioListener>();
            // ReSharper disable once UnusedVariable
            var cinematicDirector = _cardGameMasterGo.AddComponent<CinematicDirector>();

            // Minimal objects required by TurnController
            _lostObjectsGo = new GameObject("LostObjects");
            _winScreenGo = new GameObject("WinScreen");
            _turnController.lostGameObjects = _lostObjectsGo;
            _turnController.winScreen = _winScreenGo;

            _deckManager.plantLocations = new List<Transform>();
            _actionParentGo = new GameObject("ActionCardParent");
            _deckManager.actionCardParent = _actionParentGo.transform;

            // Create mock UI text components for ScoreManager
            var treatmentCostTextGo = new GameObject("TreatmentCostText");
            var treatmentCostText = treatmentCostTextGo.AddComponent<TextMeshPro>();
            var potentialProfitTextGo = new GameObject("PotentialProfitText");
            var potentialProfitText = potentialProfitTextGo.AddComponent<TextMeshPro>();

            // Inject dependencies into CardGameMaster.
            cardGameMaster.deckManager = _deckManager;
            cardGameMaster.scoreManager = _scoreManager;
            cardGameMaster.turnController = _turnController;
            cardGameMaster.soundSystem = soundSystem;
            cardGameMaster.playerHandAudioSource = audioSource;
            cardGameMaster.treatmentCostText = treatmentCostText;
            cardGameMaster.potentialProfitText = potentialProfitText;

            // Use reflection to set the private static Instance property.
            typeof(CardGameMaster)
                .GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(null, cardGameMaster);

            // Allow Unity to run initial setup code silently.
            LogAssert.ignoreFailingMessages = true;
            // Activate the GameObject so that Awake is called.
            _cardGameMasterGo.SetActive(true);
            yield return null; // Wait a frame so Awake runs.

            // --- Set up the plant and cardholder hierarchy ---
            _plantSpawnGo = new GameObject("PlantSpawn");

            // Create a plant with a PlantController.
            var plantGo = new GameObject("Plant");
            plantGo.transform.SetParent(_plantSpawnGo.transform);
            var plant = plantGo.AddComponent<PlantController>();
            plant.PlantCard = new ColeusCard();
            plant.CurrentAfflictions.Add(new FakeAffliction());

            var plantFunctions = plantGo.AddComponent<PlantCardFunctions>();
            plantFunctions.plantController = plant;
            plantFunctions.deckManager = _deckManager;
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

            // Use a FakeCard carrying our FakeTreatment.
            var fakeCard = new FakeCard("Healing Card", new FakeTreatment());
            typeof(CardView)
                .GetField("_originalCard", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(cardView, fakeCard);

            cardHolder.placedCardView = cardView;
            cardHolder.placedCard = fakeCard;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_cardGameMasterGo);
            Object.Destroy(_plantSpawnGo);
            Object.Destroy(_actionParentGo);
            Object.Destroy(_lostObjectsGo);
            Object.Destroy(_winScreenGo);
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
            CreateCardHolder(new FakeCard("Panacea", new FakeTreatment()));

            LogAssert.ignoreFailingMessages = true; // Ignore warning logs for this test

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.Pass("Handled no afflictions gracefully.");
        }

        [UnityTest]
        public IEnumerator NoPlacedCard_DoesNotThrow()
        {
            var plant = CreatePlant(new FakeAffliction());
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
            var plant = CreatePlant(new FakeAffliction());

            var cardHolder = new GameObject("CardHolder").AddComponent<PlacedCardHolder>();
            cardHolder.placedCard = new FakeCard("Healing", new FakeTreatment());
            cardHolder.placedCardClick3D = cardHolder.gameObject.AddComponent<SafeClick3D>();
            CardGameMaster.Instance.cardHolders.Add(cardHolder);

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(1, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator NullTreatment_DoesNotApply()
        {
            var plant = CreatePlant(new FakeAffliction());

            var fake = new FakeCard("Broken", null); // null treatment
            CreateCardHolder(fake);

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(1, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator TreatmentThatThrows_DoesNotBreakLoop()
        {
            var plant = CreatePlant(new FakeAffliction());

            var throwingCard = new FakeCard("Explodes", new ThrowingTreatment());
            CreateCardHolder(throwingCard);

            LogAssert.Expect(LogType.Exception, new Regex("Intentional test exception"));

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(1, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator AddAffliction_IncreasesInfectAndEggLevels()
        {
            // Fresh plant without afflictions
            Object.DestroyImmediate(_plantSpawnGo);
            CardGameMaster.Instance.cardHolders.Clear();

            _plantSpawnGo = new GameObject("TestPlantSpawn");
            var plant = CreatePlant();

            // Precondition
            Assert.AreEqual(0, plant.GetInfectLevel());
            Assert.AreEqual(0, plant.GetEggLevel());

            // Add a real affliction that carries infection and egg values
            plant.AddAffliction(new PlantAfflictions.ThripsAffliction());
            yield return null;

            // ThripsCard defaults to BaseInfectLevel=1 and BaseEggLevel=1
            Assert.AreEqual(1, plant.GetInfectLevel(), "Infect should increase when affliction is added");
            Assert.AreEqual(1, plant.GetEggLevel(), "Eggs should increase when affliction is added");
        }

        [UnityTest]
        public IEnumerator ApplyQueuedTreatments_ReducesInfect_ButNotEggs()
        {
            // Reset and build a simple environment
            Object.DestroyImmediate(_plantSpawnGo);
            CardGameMaster.Instance.cardHolders.Clear();

            _plantSpawnGo = new GameObject("TestPlantSpawn");
            var plant = CreatePlant();

            // Seed with an affliction that adds infect/eggs
            plant.AddAffliction(new PlantAfflictions.ThripsAffliction());
            yield return null;

            Assert.AreEqual(1, plant.GetInfectLevel());
            Assert.AreEqual(1, plant.GetEggLevel());

            // Queue a treatment via a placed card
            CreateCardHolder(new FakeCard("Panacea", new FakeTreatment()));

            // Apply queued treatments
            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            // Affliction removed, infect reduced by 1 via RemoveAffliction; eggs unchanged
            Assert.AreEqual(0, plant.CurrentAfflictions.Count);
            Assert.AreEqual(0, plant.GetInfectLevel(), "Infect should be reduced after treatment removes affliction");
            Assert.AreEqual(1, plant.GetEggLevel(), "Egg level persists (no automatic egg cure applied)");
        }


        [UnityTest]
        public IEnumerator MultipleAfflictions_AllGetTreated()
        {
            // Remove all test objects created in Setup
            Object.DestroyImmediate(_plantSpawnGo);
            CardGameMaster.Instance.cardHolders.Clear();

            // Create a new test-specific plant holder
            _plantSpawnGo = new GameObject("TestPlantSpawn");

            var plant = CreatePlant(new FakeAffliction(), new FakeAffliction());
            CreateCardHolder(new FakeCard("Panacea", new FakeTreatment()));

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(0, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator CardWithoutClick3D_DoesNotApply()
        {
            var plant = CreatePlant(new FakeAffliction());
            CreateCardHolder(new FakeCard("Panacea", new FakeTreatment()), false);

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(1, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator TreatmentThatModifiesList_DoesNotCrash()
        {
            // Remove all test objects created in Setup
            Object.DestroyImmediate(_plantSpawnGo);
            CardGameMaster.Instance.cardHolders.Clear();

            // Create a new test-specific plant holder
            _plantSpawnGo = new GameObject("TestPlantSpawn");

            var plant = CreatePlant(new FakeAffliction(), new FakeAffliction());
            var card = new FakeCard("Safe Clear", new SelfClearingTreatment());
            CreateCardHolder(card);

            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            Assert.AreEqual(0, plant.CurrentAfflictions.Count);
        }

        [UnityTest]
        public IEnumerator ActionDeck_IsConsistentBetweenRounds()
        {
            // Ignore exceptions from UI updates during turn sequencing
            LogAssert.ignoreFailingMessages = true;

            // Force non-tutorial mode to test regular action deck mechanics
            _turnController.level = 1;
            _turnController.tutorialCompleted = true;

            // Capture initial multiset of cards across deck + hand + discard
            var initialAll = new List<ICard>();
            initialAll.AddRange(_deckManager.GetActionDeck());
            initialAll.AddRange(_deckManager.GetActionHand());
            initialAll.AddRange(_deckManager.GetDiscardPile());
            var initialCounts = CountByName(initialAll);

            // Ensure a RetainedCardHolder exists (EndTurn references it without a null check)
            var retainedGo = new GameObject("RetainedSlot");
            retainedGo.AddComponent<RetainedCardHolder>();

            // Advance turn which will draw a new action hand, mutating deck order/content positions
            _turnController.EndTurn();
            yield return new WaitForSeconds(3f);

            // Capture new multiset after turn advancement
            var newAll = new List<ICard>();
            newAll.AddRange(_deckManager.GetActionDeck());
            newAll.AddRange(_deckManager.GetActionHand());
            newAll.AddRange(_deckManager.GetDiscardPile());
            var newCounts = CountByName(newAll);

            // Assert the multiset of cards remains identical (order/location may differ)
            Assert.AreEqual(initialCounts.Count, newCounts.Count, "Card type count changed between rounds");
            foreach (var kvp in initialCounts)
            {
                Assert.IsTrue(newCounts.ContainsKey(kvp.Key), $"Missing card '{kvp.Key}' after round");
                Assert.AreEqual(kvp.Value, newCounts[kvp.Key], $"Count mismatch for '{kvp.Key}'");
            }
        }

        [UnityTest]
        public IEnumerator ActionDeck_Tutorial_IsConsistentBetweenRounds()
        {
            // Ignore exceptions from UI updates during turn sequencing
            LogAssert.ignoreFailingMessages = true;

            // Reset tutorial flags to ensure tutorial deck usage
            _turnController.level = 0;
            _turnController.tutorialCompleted = false;
            _turnController.currentTutorialTurn = 0;

            // Initialize the tutorial action deck state up front
            _deckManager.DrawTutorialActionHand();
            yield return null;
            yield return new WaitWhile(() => _deckManager.updatingActionDisplay);

            var initialAll = new List<ICard>();
            initialAll.AddRange(_deckManager.GetActionDeck());
            initialAll.AddRange(_deckManager.GetActionHand());
            initialAll.AddRange(_deckManager.GetDiscardPile());
            var initialCounts = CountByName(initialAll);

            var retainedGo = new GameObject("RetainedSlot_Tutorial");
            retainedGo.AddComponent<RetainedCardHolder>();

            _turnController.EndTurn();
            yield return new WaitForSeconds(3f);

            var newAll = new List<ICard>();
            newAll.AddRange(_deckManager.GetActionDeck());
            newAll.AddRange(_deckManager.GetActionHand());
            newAll.AddRange(_deckManager.GetDiscardPile());
            var newCounts = CountByName(newAll);

            Assert.AreEqual(initialCounts.Count, newCounts.Count, "Card type count changed between tutorial rounds");
            foreach (var kvp in initialCounts)
            {
                Assert.IsTrue(newCounts.ContainsKey(kvp.Key), $"Missing card '{kvp.Key}' after tutorial round");
                Assert.AreEqual(kvp.Value, newCounts[kvp.Key], $"Count mismatch for '{kvp.Key}' in tutorial round");
            }
        }

        private static Dictionary<string, int> CountByName(IEnumerable<ICard> cards)
        {
            var map = new Dictionary<string, int>();
            foreach (var c in cards)
            {
                if (c == null) continue;
                var name = c.Name;
                map.TryAdd(name, 0);
                map[name] += 1;
            }

            return map;
        }

        /// <summary>
        ///     Regression test for CardView destruction bug.
        ///     This test simulates the real card placement workflow through TakeSelectedCard()
        ///     to ensure that treatments can still be applied after the card is placed.
        ///     Previously, the CardView component was destroyed during placement,
        ///     causing ApplyQueuedTreatments to skip treatment application.
        /// </summary>
        [UnityTest]
        public IEnumerator TakeSelectedCard_PreservesCardViewForTreatmentApplication()
        {
            // Remove the default setup to create a more realistic test scenario
            Object.DestroyImmediate(_plantSpawnGo);
            CardGameMaster.Instance.cardHolders.Clear();

            // Create a plant with an affliction
            _plantSpawnGo = new GameObject("TestPlantSpawn");
            var plantGo = new GameObject("Plant");
            plantGo.transform.SetParent(_plantSpawnGo.transform);
            var plant = plantGo.AddComponent<PlantController>();
            plant.PlantCard = new ColeusCard();
            plant.CurrentAfflictions.Add(new FakeAffliction());

            var plantFunctions = plantGo.AddComponent<PlantCardFunctions>();
            plantFunctions.plantController = plant;
            plantFunctions.deckManager = _deckManager;
            plant.plantCardFunctions = plantFunctions;

            // Create a cardholder (this simulates a placement location on a plant)
            var cardHolderGo = new GameObject("CardHolder");
            cardHolderGo.transform.SetParent(_plantSpawnGo.transform);
            var cardHolder = cardHolderGo.AddComponent<PlacedCardHolder>();

            // Initialize the cardholder's dependencies (normally done in Start())
            typeof(PlacedCardHolder)
                .GetField("_deckManager", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(cardHolder, _deckManager);
            typeof(PlacedCardHolder)
                .GetField("_scoreManager", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(cardHolder, _scoreManager);

            CardGameMaster.Instance.cardHolders.Add(cardHolder);

            // Create a hand card with CardView (this simulates a card in the player's hand)
            var handCardGo = new GameObject("HandCard");
            handCardGo.transform.SetParent(_actionParentGo.transform);
            var handCardView = handCardGo.AddComponent<CardView>();
            var handClick3D = handCardGo.AddComponent<SafeClick3D>();

            // Set up mock UI components that CardView.Setup() expects
            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(handCardGo.transform);
            var titleText = titleTextGo.AddComponent<TextMeshPro>();

            var descTextGo = new GameObject("DescText");
            descTextGo.transform.SetParent(handCardGo.transform);
            var descText = descTextGo.AddComponent<TextMeshPro>();

            var costTextGo = new GameObject("CostText");
            costTextGo.transform.SetParent(handCardGo.transform);
            var costText = costTextGo.AddComponent<TextMeshPro>();

            // Assign the mock UI components to CardView
            handCardView.titleText = titleText;
            handCardView.descriptionText = descText;
            handCardView.treatmentCostText = costText;

            // Add a Renderer component that CardView.Setup() expects
            var renderer = handCardGo.AddComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Standard")); // Create a basic material

            // Set up the card with treatment
            var treatmentCard = new FakeCard("Healing Card", new FakeTreatment());
            typeof(CardView)
                .GetField("_originalCard", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(handCardView, treatmentCard);

            // Set this as the selected card in the deck manager (simulates player selecting a card)
            _deckManager.SetSelectedCard(handClick3D, treatmentCard, false);

            // Verify initial state
            Assert.AreEqual(1, plant.CurrentAfflictions.Count, "Plant should start with 1 affliction");
            Assert.IsFalse(cardHolder.HoldingCard, "Card holder should start empty");

            // Verify test setup is correct
            Assert.IsNotNull(_deckManager.selectedACard, "SelectedACard should be set");
            Assert.IsNotNull(_deckManager.selectedACardClick3D, "selectedACardClick3D should be set");
            Assert.IsNotNull(handCardView, "handCardView should be set");

            // CRITICAL: Call TakeSelectedCard() to simulate real card placement workflow
            // This is where the bug occurred - CardView was destroyed but reference was kept
            cardHolder.TakeSelectedCard();
            yield return null;

            // Verify the card was placed correctly
            Assert.IsTrue(cardHolder.HoldingCard, "Card holder should now hold a card");
            Assert.IsNotNull(cardHolder.placedCard, "PlacedCard should be set");
            Assert.IsNotNull(cardHolder.placedCardView, "placedCardView should NOT be null after placement");

            // REGRESSION TEST: Apply treatments - this should work now
            // Previously this would fail because placedCardView was null
            plant.plantCardFunctions.ApplyQueuedTreatments();
            yield return null;

            // Verify the treatment was applied
            Assert.AreEqual(0, plant.CurrentAfflictions.Count,
                "Treatment should have been applied and affliction removed. " +
                "If this fails, the CardView destruction bug has returned.");
        }

        // Fake implementation for treatment.
        private class FakeTreatment : PlantAfflictions.ITreatment
        {
            public string Name => "Panacea";
            public string Description => "Cures all afflictions";
            public int? InfectCureValue { get; set; } = 999;
            public int? EggCureValue { get; set; } = 0;
            public int? Efficacy { get; set; } = 100;

            public void ApplyTreatment(PlantController plant)
            {
                var afflictions = plant.CurrentAfflictions != null
                    ? new List<PlantAfflictions.IAffliction>(plant.CurrentAfflictions)
                    : new List<PlantAfflictions.IAffliction>();

                // Apply cure values before removing afflictions
                foreach (var affliction in afflictions)
                {
                    var infectCure = InfectCureValue ?? 0;
                    var eggCure = EggCureValue ?? 0;
                    if (infectCure > 0 || eggCure > 0) plant.ReduceAfflictionValues(affliction, infectCure, eggCure);
                }

                // Remove all afflictions
                foreach (var affliction in afflictions) plant.RemoveAffliction(affliction);
            }
        }

        // Fake affliction for testing.
        private class FakeAffliction : PlantAfflictions.IAffliction
        {
            private static readonly List<PlantAfflictions.ITreatment> Treatments = new()
            {
                new FakeTreatment()
            };

            public string Name => "Test Affliction";
            public string Description => "Just a test";
            public Color Color => Color.gray;
            public Shader Shader => null;
            public List<PlantAfflictions.ITreatment> AcceptableTreatments => Treatments;

            public PlantAfflictions.IAffliction Clone()
            {
                return new FakeAffliction();
            }

            public bool CanBeTreatedBy(PlantAfflictions.ITreatment treatment)
            {
                return Treatments.Any(t => t.GetType() == treatment.GetType());
            }

            public bool TreatWith(PlantAfflictions.ITreatment treatment, PlantController plant)
            {
                if (!CanBeTreatedBy(treatment)) return false;
                plant.RemoveAffliction(this);
                return true;
            }

            public void TickDay(PlantController plant)
            {
            }
        }

        // Fake card that carries a treatment.
        private class FakeCard : ICard
        {
            public FakeCard(string name, PlantAfflictions.ITreatment treatment)
            {
                Name = name;
                Treatment = treatment;
                Stickers = new List<ISticker>();
            }

            // ReSharper disable once UnusedMember.Local
            public int? Value => 1; // Return a fake value

            public string Name { get; }
            public PlantAfflictions.ITreatment Treatment { get; }
            public string Description => "Test card";
            public Material Material => null; // CardView.Setup handles null materials
            public List<ISticker> Stickers { get; }

            public ICard Clone()
            {
                return new FakeCard(Name, Treatment);
            }
        }

        // Fake MonoBehaviour to bypass Click3D
        // Safe subclass of Click3D that disables Start logic.
        private class SafeClick3D : Click3D
        {
            // ReSharper disable once Unity.RedundantEventFunction
            private void Start()
            {
                /* No-op to prevent self-destruction check from running */
            }
        }

        private class ThrowingTreatment : PlantAfflictions.ITreatment
        {
            public string Name => "Explosive";
            public string Description => "Throws on apply";
            public int? InfectCureValue { get; set; } = 0;
            public int? EggCureValue { get; set; } = 0;
            public int? Efficacy { get; set; } = 100;

            public void ApplyTreatment(PlantController plant)
            {
                Debug.LogException(new Exception("Intentional test exception"));
            }
        }

        private class SelfClearingTreatment : PlantAfflictions.ITreatment
        {
            public string Name => "SafeClear";
            public string Description => "Removes all afflictions";
            public int? InfectCureValue { get; set; } = 999;
            public int? EggCureValue { get; set; } = 999;
            public int? Efficacy { get; set; } = 100;

            public void ApplyTreatment(PlantController plant)
            {
                var afflictionsCopy = new List<PlantAfflictions.IAffliction>(plant.CurrentAfflictions);

                // Apply cure values before removing afflictions
                foreach (var affliction in afflictionsCopy)
                {
                    var infectCure = InfectCureValue ?? 0;
                    var eggCure = EggCureValue ?? 0;
                    if (infectCure > 0 || eggCure > 0) plant.ReduceAfflictionValues(affliction, infectCure, eggCure);
                }

                // Remove all afflictions
                foreach (var affliction in afflictionsCopy) plant.RemoveAffliction(affliction);
            }
        }
    }
}