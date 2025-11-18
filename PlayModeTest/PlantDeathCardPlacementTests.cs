using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    public class PlantDeathCardPlacementTests
    {
        private GameObject _cardGameMasterGo;
        private GameObject _plantLocationGo;
        private DeckManager _deckManager;
        private ScoreManager _scoreManager;
        private TurnController _turnController;
        private GameObject _lostObjectsGo;
        private GameObject _winScreenGo;
        private GameObject _actionParentGo;

        // Fake card for testing
        private class FakeCard : ICard
        {
            public string Name => "Test Card";
            public PlantAfflictions.ITreatment Treatment => null;
            public string Description => "Test card for death placement tests";
            public int? Value => 1;
            public Material Material => null;
            public List<ISticker> Stickers { get; } = new List<ISticker>();
            public ICard Clone() => new FakeCard();
        }

        // Safe Click3D subclass that disables Start logic
        private class SafeClick3D : Click3D
        {
            private void Start() { /* No-op to prevent self-destruction check */ }
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Create CardGameMaster GameObject inactive to prevent early Awake
            _cardGameMasterGo = new GameObject("CardGameMaster");
            _cardGameMasterGo.SetActive(false);

            // Add required components
            _deckManager = _cardGameMasterGo.AddComponent<DeckManager>();
            _scoreManager = _cardGameMasterGo.AddComponent<ScoreManager>();
            _turnController = _cardGameMasterGo.AddComponent<TurnController>();
            var cardGameMaster = _cardGameMasterGo.AddComponent<CardGameMaster>();
            var soundSystem = _cardGameMasterGo.AddComponent<SoundSystemMaster>();
            var audioSource = _cardGameMasterGo.AddComponent<AudioSource>();
            _cardGameMasterGo.AddComponent<AudioListener>();
            _cardGameMasterGo.AddComponent<CinematicDirector>();

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

            // Inject dependencies into CardGameMaster
            cardGameMaster.deckManager = _deckManager;
            cardGameMaster.scoreManager = _scoreManager;
            cardGameMaster.turnController = _turnController;
            cardGameMaster.soundSystem = soundSystem;
            cardGameMaster.playerHandAudioSource = audioSource;
            cardGameMaster.treatmentCostText = treatmentCostText;
            cardGameMaster.potentialProfitText = potentialProfitText;

            // Set the static Instance property
            typeof(CardGameMaster)
                .GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(null, cardGameMaster);

            // Suppress expected warnings during setup
            LogAssert.ignoreFailingMessages = true;
            _cardGameMasterGo.SetActive(true);
            yield return null; // Wait for Awake to run

            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_cardGameMasterGo);
            Object.Destroy(_plantLocationGo);
            Object.Destroy(_actionParentGo);
            Object.Destroy(_lostObjectsGo);
            Object.Destroy(_winScreenGo);
            yield return null;
        }

        private PlantController CreatePlantWithCardHolder(out PlacedCardHolder cardHolder)
        {
            // Create plant location (parent GameObject)
            _plantLocationGo = new GameObject("PlantLocation");

            // Create plant
            var plantGo = new GameObject("Plant");
            plantGo.transform.SetParent(_plantLocationGo.transform);
            var plant = plantGo.AddComponent<PlantController>();
            plant.PlantCard = new ColeusCard { Value = 10 }; // Start with healthy plant

            var plantFunctions = plantGo.AddComponent<PlantCardFunctions>();
            plantFunctions.plantController = plant;
            plantFunctions.deckManager = _deckManager;
            plant.plantCardFunctions = plantFunctions;

            // Create cardholder
            var cardHolderGo = new GameObject("CardHolder");
            cardHolderGo.transform.SetParent(_plantLocationGo.transform);
            cardHolder = cardHolderGo.AddComponent<PlacedCardHolder>();

            // Add Click3D component (will be found dynamically by ToggleCardHolder)
            cardHolderGo.AddComponent<SafeClick3D>();

            // Register location with DeckManager
            _deckManager.plantLocations.Add(_plantLocationGo.transform);

            return plant;
        }

        [UnityTest]
        public IEnumerator CardHolder_DisabledWhenPlantDies()
        {
            // Arrange
            var plant = CreatePlantWithCardHolder(out var cardHolder);

            // Enable the cardholder initially
            cardHolder.ToggleCardHolder(true);
            yield return null;

            // Verify holder is initially enabled
            var holderClick3D = cardHolder.gameObject.GetComponentInChildren<Click3D>(true);
            Assert.IsNotNull(holderClick3D, "Click3D component should exist on card holder");
            Assert.IsTrue(holderClick3D.isEnabled, "Card holder should be enabled initially");

            // Act - kill the plant by setting value to 0
            plant.PlantCard.Value = 0;
            yield return null; // Wait for Update() to detect death

            // Assert - cardholder should be disabled immediately
            Assert.IsFalse(holderClick3D.isEnabled, "Card holder should be disabled when plant dies");
        }

        [UnityTest]
        public IEnumerator PlacedCard_ClearedByDeckManagerClearPlant()
        {
            // Arrange
            var plant = CreatePlantWithCardHolder(out var cardHolder);

            // Manually place a card on the holder (simulating a card that was placed before death)
            var cardGo = new GameObject("PlacedCard");
            cardGo.transform.SetParent(cardHolder.transform);
            var cardView = cardGo.AddComponent<CardView>();
            var cardClick3D = cardGo.AddComponent<SafeClick3D>();

            var testCard = new FakeCard();
            typeof(CardView)
                .GetField("_originalCard", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(cardView, testCard);

            cardHolder.placedCard = testCard;
            cardHolder.placedCardClick3D = cardClick3D;
            cardHolder.placedCardView = cardView;

            yield return null;

            // Verify card is placed
            Assert.IsTrue(cardHolder.HoldingCard, "Card should be placed initially");

            // Act - call ClearPlant directly (this is what happens after death animation)
            yield return _deckManager.ClearPlant(plant, skipDeathSequence: true);

            // Assert - card should be cleared by DeckManager.ClearPlant()
            Assert.IsFalse(cardHolder.HoldingCard, "Card should be cleared when DeckManager.ClearPlant() is called");
            Assert.IsNull(cardHolder.placedCard, "Placed card reference should be null");
            Assert.IsNull(cardHolder.placedCardClick3D, "Placed card Click3D should be null");
        }

        [UnityTest]
        public IEnumerator ClearPlant_RemovesAllCardsFromHolders()
        {
            // Arrange
            var plant = CreatePlantWithCardHolder(out var cardHolder);

            // Place a card on the holder
            var cardGo = new GameObject("PlacedCard");
            cardGo.transform.SetParent(cardHolder.transform);
            var cardView = cardGo.AddComponent<CardView>();
            var cardClick3D = cardGo.AddComponent<SafeClick3D>();

            var testCard = new FakeCard();
            typeof(CardView)
                .GetField("_originalCard", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(cardView, testCard);

            cardHolder.placedCard = testCard;
            cardHolder.placedCardClick3D = cardClick3D;
            cardHolder.placedCardView = cardView;

            yield return null;

            // Act - call ClearPlant directly (skipping death animation)
            yield return _deckManager.ClearPlant(plant, skipDeathSequence: true);

            // Assert - cardholder should be cleared and disabled
            Assert.IsFalse(cardHolder.HoldingCard, "Card holder should not hold a card after ClearPlant");

            var holderClick3D = cardHolder.gameObject.GetComponentInChildren<Click3D>(true);
            Assert.IsNotNull(holderClick3D, "Click3D component should exist on card holder");
            Assert.IsFalse(holderClick3D.isEnabled, "Card holder should be disabled after ClearPlant");
        }
    }
}
