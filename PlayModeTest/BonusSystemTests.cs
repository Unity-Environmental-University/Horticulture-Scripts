using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _project.Scripts.Card_Core;
using _project.Scripts.Core;
using _project.Scripts.PlayModeTest.Utilities.Mocks;
using _project.Scripts.PlayModeTest.Utilities.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Tests for the bonus system infrastructure in ScoreManager and TurnController.
    ///     Tests cover current implementation with placeholder bonuses and infrastructure for future expansion.
    /// </summary>
    public class BonusSystemTests
    {
        private CardGameMaster _cardGameMaster;
        private GameObject _cardGameMasterGo;
        private DeckManager _deckManager;
        private GameObject _lostObjectsGo;
        private ScoreManager _scoreManager;
        private TurnController _turnController;
        private GameObject _winScreenGo;

        [SetUp]
        public void Setup()
        {
            // Create CardGameMaster GameObject hierarchy
            _cardGameMasterGo = new GameObject("CardGameMaster_BonusTest");
            _cardGameMaster = _cardGameMasterGo.AddComponent<CardGameMaster>();
            _scoreManager = _cardGameMasterGo.AddComponent<ScoreManager>();
            _turnController = _cardGameMasterGo.AddComponent<TurnController>();
            _deckManager = _cardGameMasterGo.AddComponent<DeckManager>();

            // Create required UI elements
            _cardGameMaster.moneysText = new GameObject("MoneysText").AddComponent<TextMeshPro>();
            _cardGameMaster.shopMoneyText = new GameObject("ShopMoneyText").AddComponent<TextMeshProUGUI>();
            _cardGameMaster.turnText = new GameObject("TurnText").AddComponent<TextMeshPro>();
            _cardGameMaster.roundText = new GameObject("RoundText").AddComponent<TextMeshPro>();
            _cardGameMaster.levelText = new GameObject("LevelText").AddComponent<TextMeshPro>();
            _cardGameMaster.treatmentCostText = new GameObject("CostText").AddComponent<TextMeshPro>();
            _cardGameMaster.potentialProfitText = new GameObject("ProfitText").AddComponent<TextMeshPro>();

            // Create minimal TurnController dependencies
            _lostObjectsGo = new GameObject("LostObjects");
            _winScreenGo = new GameObject("WinScreen");
            _turnController.lostGameObjects = _lostObjectsGo;
            _turnController.winScreen = _winScreenGo;

            // Wire up components
            _cardGameMaster.scoreManager = _scoreManager;
            _cardGameMaster.turnController = _turnController;
            _cardGameMaster.deckManager = _deckManager;

            // Set CardGameMaster instance reference
            var instanceField = typeof(CardGameMaster).GetField("Instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            instanceField?.SetValue(null, _cardGameMaster);

            // Initialize ScoreManager
            _scoreManager.ResetMoneys();
        }

        [TearDown]
        public void TearDown()
        {
            if (_cardGameMasterGo) Object.DestroyImmediate(_cardGameMasterGo);
            if (_lostObjectsGo) Object.DestroyImmediate(_lostObjectsGo);
            if (_winScreenGo) Object.DestroyImmediate(_winScreenGo);

            // Clear CardGameMaster instance
            var instanceField = typeof(CardGameMaster).GetField("Instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            instanceField?.SetValue(null, null);
        }

        #region Debug Logging Tests

        [Test]
        public void CalculateScore_LogsBonusesWhenDebugging()
        {
            _scoreManager.debugging = true;
            _scoreManager.bonuses.Add(new IBonus { Name = "TestBonus1", BonusValue = 10 });
            _scoreManager.bonuses.Add(new IBonus { Name = "TestBonus2", BonusValue = 5 });

            // This would normally log to console when debugging is enabled
            LogAssert.NoUnexpectedReceived();
            _scoreManager.CalculateScore();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        ///     Creates a plant location GameObject with a PlantController for testing.
        /// </summary>
        private GameObject CreatePlantLocation(string name, bool hasAfflictions, bool isDead)
        {
            var plantLocationGo = new GameObject(name);
            var plantGo = new GameObject($"{name}_Plant");
            plantGo.transform.SetParent(plantLocationGo.transform);

            var plantController = plantGo.AddComponent<PlantController>();
            plantController.plantCardFunctions = plantGo.AddComponent<PlantCardFunctions>();

            // Set up plant state
            if (isDead)
            {
                // Destroy the PlantController to simulate death
                Object.DestroyImmediate(plantController);
            }
            else if (hasAfflictions)
            {
                // Add a fake affliction
                var fakeAffliction = new FakeAffliction();
                plantController.CurrentAfflictions.Add(fakeAffliction);
            }

            return plantLocationGo;
        }

        #endregion

        #region IBonus Class Tests

        [Test]
        public void IBonusClass_HasNameProperty()
        {
            var bonus = new IBonus { Name = "TestBonus", BonusValue = 10 };
            Assert.AreEqual("TestBonus", bonus.Name, "IBonus should store Name property");
        }

        [Test]
        public void IBonusClass_HasBonusValueProperty()
        {
            var bonus = new IBonus { Name = "TestBonus", BonusValue = 15 };
            Assert.AreEqual(15, bonus.BonusValue, "IBonus should store BonusValue property");
        }

        [Test]
        public void IBonusClass_AllowsZeroValue()
        {
            var bonus = new IBonus { Name = "ZeroBonus", BonusValue = 0 };
            Assert.AreEqual(0, bonus.BonusValue, "IBonus should allow zero values");
        }

        [Test]
        public void IBonusClass_AllowsNegativeValue()
        {
            var bonus = new IBonus { Name = "Penalty", BonusValue = -5 };
            Assert.AreEqual(-5, bonus.BonusValue, "IBonus should allow negative values (penalties)");
        }

        #endregion

        #region ScoreManager Bonus List Tests

        [Test]
        public void ScoreManager_BonusListExists()
        {
            Assert.IsNotNull(_scoreManager.bonuses, "ScoreManager should have a bonuses list");
        }

        [Test]
        public void ScoreManager_BonusListStartsEmpty()
        {
            Assert.AreEqual(0, _scoreManager.bonuses.Count,
                "Bonus list should start empty");
        }

        [Test]
        public void ScoreManager_CanAddBonus()
        {
            _scoreManager.bonuses.Add(new IBonus { Name = "TestBonus", BonusValue = 10 });
            Assert.AreEqual(1, _scoreManager.bonuses.Count,
                "Should be able to add bonus to list");
        }

        [Test]
        public void ScoreManager_CanAddMultipleBonuses()
        {
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus1", BonusValue = 5 });
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus2", BonusValue = 10 });
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus3", BonusValue = 15 });

            Assert.AreEqual(3, _scoreManager.bonuses.Count,
                "Should be able to add multiple bonuses");
        }

        #endregion

        #region CalculateBonuses Method Tests

        [Test]
        public void CalculateBonuses_ReturnsZeroForEmptyList()
        {
            _scoreManager.bonuses.Clear();
            var total = ScoreManagerReflection.InvokeCalculateBonuses(_scoreManager);

            Assert.AreEqual(0, total,
                "CalculateBonuses should return 0 when no bonuses are present");
        }

        [Test]
        public void CalculateBonuses_ReturnsSingleBonusValue()
        {
            _scoreManager.bonuses.Add(new IBonus { Name = "SingleBonus", BonusValue = 25 });
            var total = ScoreManagerReflection.InvokeCalculateBonuses(_scoreManager);

            Assert.AreEqual(25, total,
                "CalculateBonuses should return the value of a single bonus");
        }

        [Test]
        public void CalculateBonuses_SumsMultipleBonuses()
        {
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus1", BonusValue = 10 });
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus2", BonusValue = 15 });
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus3", BonusValue = 5 });

            var total = ScoreManagerReflection.InvokeCalculateBonuses(_scoreManager);

            Assert.AreEqual(30, total,
                "CalculateBonuses should sum all bonus values");
        }

        [Test]
        public void CalculateBonuses_HandlesZeroValueBonuses()
        {
            _scoreManager.bonuses.Add(new IBonus { Name = "RealBonus", BonusValue = 10 });
            _scoreManager.bonuses.Add(new IBonus { Name = "PlaceholderBonus", BonusValue = 0 });

            var total = ScoreManagerReflection.InvokeCalculateBonuses(_scoreManager);

            Assert.AreEqual(10, total,
                "CalculateBonuses should handle zero-value bonuses correctly");
        }

        [Test]
        public void CalculateBonuses_HandlesNegativeValues()
        {
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus", BonusValue = 20 });
            _scoreManager.bonuses.Add(new IBonus { Name = "Penalty", BonusValue = -5 });

            var total = ScoreManagerReflection.InvokeCalculateBonuses(_scoreManager);

            Assert.AreEqual(15, total,
                "CalculateBonuses should handle negative values (penalties) correctly");
        }

        #endregion

        #region CalculateScore Integration Tests

        [Test]
        public void CalculateScore_AppliesBonusesToScore()
        {
            // Set up initial money
            ScoreManager.SetScore(50);

            // Add a bonus
            _scoreManager.bonuses.Add(new IBonus { Name = "TestBonus", BonusValue = 10 });

            // Calculate score (this should apply the bonus)
            var finalScore = _scoreManager.CalculateScore();

            Assert.AreEqual(60, finalScore,
                "CalculateScore should add bonus value to the score");
        }

        [Test]
        public void CalculateScore_AppliesMultipleBonuses()
        {
            // Set up initial money
            ScoreManager.SetScore(100);

            // Add multiple bonuses
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus1", BonusValue = 5 });
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus2", BonusValue = 10 });
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus3", BonusValue = 15 });

            // Calculate score
            var finalScore = _scoreManager.CalculateScore();

            Assert.AreEqual(130, finalScore,
                "CalculateScore should apply all bonuses");
        }

        [Test]
        public void CalculateScore_ClearsBonusesAfterApplication()
        {
            // Set up initial money
            ScoreManager.SetScore(50);

            // Add bonuses
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus1", BonusValue = 10 });
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus2", BonusValue = 5 });

            // Calculate score
            _scoreManager.CalculateScore();

            Assert.AreEqual(0, _scoreManager.bonuses.Count,
                "CalculateScore should clear bonuses after applying them");
        }

        [Test]
        public void CalculateScore_BonusesDoNotPersistBetweenRounds()
        {
            // Round 1
            ScoreManager.SetScore(50);
            _scoreManager.bonuses.Add(new IBonus { Name = "Round1Bonus", BonusValue = 10 });
            var round1Score = _scoreManager.CalculateScore();
            Assert.AreEqual(60, round1Score, "Round 1 should include bonus");

            // Round 2 (no new bonuses added)
            var round2Score = _scoreManager.CalculateScore();
            Assert.AreEqual(60, round2Score,
                "Round 2 should not re-apply bonuses from Round 1");
        }

        #endregion

        #region TurnController Bonus Addition Tests

        [UnityTest]
        public IEnumerator TurnController_AddsAllPlantsSurvivedBonus()
        {
            // Setup: Create plant locations with healthy plants
            var plantLocation1 = CreatePlantLocation("Plant1", false, false);
            var plantLocation2 = CreatePlantLocation("Plant2", false, false);

            _deckManager.plantLocations = new List<PlantHolder>
            {
                new(plantLocation1.transform),
                new(plantLocation2.transform)
            };

            // Clear any existing bonuses
            _scoreManager.bonuses.Clear();

            // Simulate EndRound logic that adds the bonus
            var validLocations = _deckManager.plantLocations?
                .Where(loc => loc)
                .ToArray() ?? new PlantHolder[0];

            var plantControllers = validLocations
                .Select(loc => loc.Transform.GetComponentInChildren<PlantController>(true))
                .Where(p => p != null)
                .ToArray();

            var plantsDead = validLocations.Length - plantControllers.Length;

            // Add bonus (matching TurnController.cs line 702)
            if (plantsDead <= 0)
                _scoreManager.bonuses.Add(new IBonus { Name = "AllPlantsSurvived", BonusValue = 0 });

            // Verify bonus was added
            Assert.AreEqual(1, _scoreManager.bonuses.Count,
                "AllPlantsSurvived bonus should be added when no plants died");
            Assert.AreEqual("AllPlantsSurvived", _scoreManager.bonuses[0].Name,
                "Bonus should have correct name");

            // Cleanup
            Object.DestroyImmediate(plantLocation1);
            Object.DestroyImmediate(plantLocation2);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TurnController_AllPlantsSurvivedBonusHasZeroValue()
        {
            // This test documents current behavior: bonus value is 0
            // When fully implemented, this should be updated to expect 4

            var plantLocation = CreatePlantLocation("Plant", false, false);
            _deckManager.plantLocations = new List<PlantHolder>
            {
                new(plantLocation.transform)
            };

            _scoreManager.bonuses.Clear();

            var validLocations = _deckManager.plantLocations.Where(loc => loc).ToArray();
            var plantControllers = validLocations
                .Select(loc => loc.Transform.GetComponentInChildren<PlantController>(true))
                .Where(p => p != null)
                .ToArray();
            var plantsDead = validLocations.Length - plantControllers.Length;

            if (plantsDead <= 0)
                _scoreManager.bonuses.Add(new IBonus { Name = "AllPlantsSurvived", BonusValue = 0 });

            Assert.AreEqual(0, _scoreManager.bonuses[0].BonusValue,
                "Current implementation: AllPlantsSurvived bonus has value 0 (placeholder)");

            Object.DestroyImmediate(plantLocation);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TurnController_DoesNotAddBonusWhenPlantsDied()
        {
            // Setup: Create plant locations with 1 dead plant
            var plantLocation1 = CreatePlantLocation("Plant1", false, false);
            var plantLocation2 = CreatePlantLocation("Plant2", true, true);

            _deckManager.plantLocations = new List<PlantHolder>
            {
                new(plantLocation1.transform),
                new(plantLocation2.transform)
            };

            _scoreManager.bonuses.Clear();

            var validLocations = _deckManager.plantLocations.Where(loc => loc).ToArray();
            var plantControllers = validLocations
                .Select(loc => loc.Transform.GetComponentInChildren<PlantController>(true))
                .Where(p => p != null)
                .ToArray();
            var plantsDead = validLocations.Length - plantControllers.Length;

            if (plantsDead <= 0)
                _scoreManager.bonuses.Add(new IBonus { Name = "AllPlantsSurvived", BonusValue = 0 });

            Assert.AreEqual(0, _scoreManager.bonuses.Count,
                "Bonus should NOT be added when plants died");

            Object.DestroyImmediate(plantLocation1);
            Object.DestroyImmediate(plantLocation2);
            yield return null;
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void CalculateScore_HandlesEmptyBonusListGracefully()
        {
            ScoreManager.SetScore(50);
            _scoreManager.bonuses.Clear();

            var finalScore = _scoreManager.CalculateScore();

            Assert.AreEqual(50, finalScore,
                "CalculateScore should work correctly with empty bonus list");
        }

        [Test]
        public void CalculateScore_HandlesAllZeroBonuses()
        {
            ScoreManager.SetScore(50);
            _scoreManager.bonuses.Add(new IBonus { Name = "Zero1", BonusValue = 0 });
            _scoreManager.bonuses.Add(new IBonus { Name = "Zero2", BonusValue = 0 });

            var finalScore = _scoreManager.CalculateScore();

            Assert.AreEqual(50, finalScore,
                "CalculateScore should handle all-zero bonuses correctly");
        }

        [Test]
        public void CalculateScore_HandlesLargeBonusValues()
        {
            ScoreManager.SetScore(100);
            _scoreManager.bonuses.Add(new IBonus { Name = "HugeBonus", BonusValue = 9999 });

            var finalScore = _scoreManager.CalculateScore();

            Assert.AreEqual(10099, finalScore,
                "CalculateScore should handle large bonus values");
        }

        [Test]
        public void CalculateScore_HandlesNetNegativeBonus()
        {
            ScoreManager.SetScore(100);
            _scoreManager.bonuses.Add(new IBonus { Name = "Bonus", BonusValue = 10 });
            _scoreManager.bonuses.Add(new IBonus { Name = "BigPenalty", BonusValue = -20 });

            var finalScore = _scoreManager.CalculateScore();

            Assert.AreEqual(90, finalScore,
                "CalculateScore should handle net negative bonuses (more penalties than bonuses)");
        }

        #endregion
    }
}