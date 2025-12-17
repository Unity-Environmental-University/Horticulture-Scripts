using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Card_Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Tests for PrepareNextRound method to ensure it doesn't trigger premature game loss
    ///     based on negative balance. Players should only lose at the rent check in Campaign mode.
    /// </summary>
    public class PrepareNextRoundTests
    {
        private CardGameMaster _cardGameMaster;
        private GameObject _cardGameMasterGo;
        private DeckManager _deckManager;
        private GameObject _lostGameObjects;
        private ScoreManager _scoreManager;
        private TurnController _turnController;

        [SetUp]
        public void Setup()
        {
            // Create CardGameMaster GameObject INACTIVE to prevent early Awake
            _cardGameMasterGo = new GameObject("CardGameMaster_Test");
            _cardGameMasterGo.SetActive(false);

            // Add components while inactive
            _cardGameMaster = _cardGameMasterGo.AddComponent<CardGameMaster>();
            _turnController = _cardGameMasterGo.AddComponent<TurnController>();
            _scoreManager = _cardGameMasterGo.AddComponent<ScoreManager>();
            _deckManager = _cardGameMasterGo.AddComponent<DeckManager>();

            // Create required UI elements
            _cardGameMaster.moneysText = new GameObject("MoneyText").AddComponent<TextMeshPro>();
            _cardGameMaster.treatmentCostText = new GameObject("TreatmentCostText").AddComponent<TextMeshPro>();
            _cardGameMaster.potentialProfitText = new GameObject("PotentialProfitText").AddComponent<TextMeshPro>();
            _cardGameMaster.turnText = new GameObject("TurnText").AddComponent<TextMeshPro>();
            _cardGameMaster.roundText = new GameObject("RoundText").AddComponent<TextMeshPro>();
            _cardGameMaster.levelText = new GameObject("LevelText").AddComponent<TextMeshPro>();

            // Create lost game objects
            _lostGameObjects = new GameObject("LostGameObjects");
            _lostGameObjects.SetActive(false);
            _turnController.lostGameObjects = _lostGameObjects;

            // Initialize deck manager with empty plant locations
            _deckManager.plantLocations = new List<PlantHolder>();

            // Create action card parent (required by DeckManager)
            var actionParentGo = new GameObject("ActionCardParent");
            _deckManager.actionCardParent = actionParentGo.transform;

            // Wire up components
            _cardGameMaster.turnController = _turnController;
            _cardGameMaster.scoreManager = _scoreManager;
            _cardGameMaster.deckManager = _deckManager;

            // Set CardGameMaster instance
            var instanceField = typeof(CardGameMaster).GetField("Instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            instanceField?.SetValue(null, _cardGameMaster);

            // Activate GameObject to trigger Awake methods
            _cardGameMasterGo.SetActive(true);

            // Initialize score manager
            _scoreManager.ResetMoneys();

            // Set up campaign mode by default
            _turnController.level = 1;
            _turnController.currentGameMode = GameMode.Campaign;
            _turnController.currentRoundInLevel = 0;
            _turnController.moneyGoal = 100;
        }

        [TearDown]
        public void TearDown()
        {
            if (_cardGameMasterGo) Object.DestroyImmediate(_cardGameMasterGo);
            if (_lostGameObjects) Object.DestroyImmediate(_lostGameObjects);

            // Clear CardGameMaster instance
            var instanceField = typeof(CardGameMaster).GetField("Instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            instanceField?.SetValue(null, null);
        }

        /// <summary>
        ///     Tests that PrepareNextRound with negative score does NOT trigger game loss.
        ///     This is the primary bug fix - players should be able to have negative balance
        ///     between rounds in Campaign mode.
        /// </summary>
        [Test]
        public void PrepareNextRound_WithNegativeScore_DoesNotTriggerLoss()
        {
            // Set up campaign mode at Round 2 (mid-cycle)
            _turnController.currentRoundInLevel = 2;
            _turnController.currentGameMode = GameMode.Campaign;

            // Call PrepareNextRound with negative score using reflection
            var method = typeof(TurnController).GetMethod("PrepareNextRound",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_turnController, new object[] { -50, false });

            // Loss screen should NOT be active
            Assert.IsFalse(_lostGameObjects.activeSelf,
                "PrepareNextRound should NOT trigger game loss for negative score");

            // Round should be ready to continue
            Assert.IsTrue(_turnController.newRoundReady,
                "Round should be ready to continue even with negative score");
            Assert.IsTrue(_turnController.canClickEnd,
                "Player should be able to proceed even with negative score");
        }

        /// <summary>
        ///     Tests that PrepareNextRound with zero score does NOT trigger game loss.
        /// </summary>
        [Test]
        public void PrepareNextRound_WithZeroScore_DoesNotTriggerLoss()
        {
            // Set up campaign mode
            _turnController.currentRoundInLevel = 3;
            _turnController.currentGameMode = GameMode.Campaign;

            // Call PrepareNextRound with zero score
            var method = typeof(TurnController).GetMethod("PrepareNextRound",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_turnController, new object[] { 0, false });

            // Loss screen should NOT be active
            Assert.IsFalse(_lostGameObjects.activeSelf,
                "PrepareNextRound should NOT trigger game loss for zero score");

            // Round should be ready to continue
            Assert.IsTrue(_turnController.newRoundReady,
                "Round should be ready to continue with zero score");
            Assert.IsTrue(_turnController.canClickEnd,
                "Player should be able to proceed with zero score");
        }

        /// <summary>
        ///     Tests that PrepareNextRound with positive score allows normal progression.
        /// </summary>
        [Test]
        public void PrepareNextRound_WithPositiveScore_AllowsProgression()
        {
            // Set up campaign mode
            _turnController.currentRoundInLevel = 1;
            _turnController.currentGameMode = GameMode.Campaign;

            // Call PrepareNextRound with positive score
            var method = typeof(TurnController).GetMethod("PrepareNextRound",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_turnController, new object[] { 150, false });

            // Loss screen should NOT be active
            Assert.IsFalse(_lostGameObjects.activeSelf,
                "PrepareNextRound should not trigger game loss for positive score");

            // Round should be ready to continue
            Assert.IsTrue(_turnController.newRoundReady,
                "Round should be ready to continue with positive score");
            Assert.IsTrue(_turnController.canClickEnd,
                "Player should be able to proceed with positive score");
        }

        /// <summary>
        ///     Tests that tutorial advancement works correctly when advanceTutorial=true.
        /// </summary>
        [Test]
        public void PrepareNextRound_AdvanceTutorial_IncrementsCounter()
        {
            // Set up tutorial mode
            _turnController.level = 0; // Tutorial level
            _turnController.currentTutorialTurn = 0;

            // Enable sequencing via PlayerPrefs (IsSequencingEnabled reads from PlayerPrefs)
            PlayerPrefs.SetInt("Tutorial", 1);

            // Call PrepareNextRound with advanceTutorial=true
            var method = typeof(TurnController).GetMethod("PrepareNextRound",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_turnController, new object[] { 100, true });

            // Tutorial turn should advance
            Assert.AreEqual(1, _turnController.currentTutorialTurn,
                "Tutorial turn should increment when advanceTutorial=true");

            // Round should be ready
            Assert.IsTrue(_turnController.newRoundReady,
                "Round should be ready after tutorial advancement");
            Assert.IsTrue(_turnController.canClickEnd,
                "Player should be able to proceed after tutorial advancement");

            // Cleanup: reset PlayerPrefs
            PlayerPrefs.DeleteKey("Tutorial");
        }

        /// <summary>
        ///     Tests that tutorial counter does NOT increment when advanceTutorial=false.
        /// </summary>
        [Test]
        public void PrepareNextRound_AdvanceTutorialFalse_DoesNotIncrementCounter()
        {
            // Set up tutorial mode
            _turnController.level = 0; // Tutorial level
            _turnController.currentTutorialTurn = 0;

            // Enable sequencing via PlayerPrefs
            PlayerPrefs.SetInt("Tutorial", 1);

            // Call PrepareNextRound with advanceTutorial=false
            var method = typeof(TurnController).GetMethod("PrepareNextRound",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_turnController, new object[] { 100, false });

            // Tutorial turn should NOT advance
            Assert.AreEqual(0, _turnController.currentTutorialTurn,
                "Tutorial turn should NOT increment when advanceTutorial=false");

            // Round should still be ready
            Assert.IsTrue(_turnController.newRoundReady,
                "Round should be ready even without tutorial advancement");

            // Cleanup
            PlayerPrefs.DeleteKey("Tutorial");
        }

        /// <summary>
        ///     Tests that PrepareNextRound works correctly in Tutorial mode with negative score.
        ///     Tutorial mode should never trigger loss based on money.
        /// </summary>
        [Test]
        public void PrepareNextRound_TutorialMode_NegativeScore_DoesNotTriggerLoss()
        {
            // Set up tutorial mode
            _turnController.level = 0;
            _turnController.currentGameMode = GameMode.Tutorial;

            // Call PrepareNextRound with negative score
            var method = typeof(TurnController).GetMethod("PrepareNextRound",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_turnController, new object[] { -100, false });

            // Loss screen should NOT be active
            Assert.IsFalse(_lostGameObjects.activeSelf,
                "Tutorial mode should NEVER trigger loss based on money");

            // Round should be ready
            Assert.IsTrue(_turnController.newRoundReady,
                "Tutorial mode should continue regardless of money");
        }

        /// <summary>
        ///     Tests that PrepareNextRound works correctly in Endless mode with negative score.
        ///     Endless mode should never trigger loss based on money.
        /// </summary>
        [Test]
        public void PrepareNextRound_EndlessMode_NegativeScore_DoesNotTriggerLoss()
        {
            // Set up endless mode
            _turnController.level = 1;
            _turnController.currentGameMode = GameMode.Endless;

            // Call PrepareNextRound with negative score
            var method = typeof(TurnController).GetMethod("PrepareNextRound",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_turnController, new object[] { -75, false });

            // Loss screen should NOT be active
            Assert.IsFalse(_lostGameObjects.activeSelf,
                "Endless mode should NEVER trigger loss based on money");

            // Round should be ready
            Assert.IsTrue(_turnController.newRoundReady,
                "Endless mode should continue regardless of money");
        }

        /// <summary>
        ///     Tests that round state flags are correctly set regardless of score.
        /// </summary>
        [Test]
        public void PrepareNextRound_AlwaysSetsRoundStateFlags()
        {
            // Test with various scores
            var testScores = new[] { -100, -1, 0, 1, 100 };

            foreach (var score in testScores)
            {
                // Reset state
                _turnController.newRoundReady = false;
                _turnController.canClickEnd = false;

                // Call PrepareNextRound
                var method = typeof(TurnController).GetMethod("PrepareNextRound",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(_turnController, new object[] { score, false });

                // Verify flags are set
                Assert.IsTrue(_turnController.newRoundReady,
                    $"newRoundReady should be true for score={score}");
                Assert.IsTrue(_turnController.canClickEnd,
                    $"canClickEnd should be true for score={score}");
            }
        }

        /// <summary>
        ///     Integration test: Verifies that the rent check (not PrepareNextRound) is the
        ///     correct place for Campaign mode loss based on insufficient funds.
        /// </summary>
        [Test]
        public void CampaignMode_LossOnlyAtRentCheck_NotInPrepareNextRound()
        {
            // Set up campaign mode at Round 5 (rent check time)
            _turnController.currentRoundInLevel = 5;
            _turnController.currentGameMode = GameMode.Campaign;
            _turnController.moneyGoal = 100;
            ScoreManager.SetScore(50); // Insufficient for rent

            // Call PrepareNextRound with negative score (mid-cycle simulation)
            var method = typeof(TurnController).GetMethod("PrepareNextRound",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(_turnController, new object[] { 50, false });

            // PrepareNextRound should NOT trigger loss
            Assert.IsFalse(_lostGameObjects.activeSelf,
                "PrepareNextRound should NOT trigger loss even at Round 5");

            // Verify rent check logic (this is where loss SHOULD happen)
            var canAffordRent = ScoreManager.GetMoneys() >= _turnController.moneyGoal;
            Assert.IsFalse(canAffordRent,
                "Player should fail rent check with $50 when rent is $100");

            // Note: The actual GameLost() call happens in EndRound coroutine at the rent check,
            // which we can't easily test in a unit test. This test verifies that PrepareNextRound
            // does NOT trigger the loss, leaving it to the correct rent check logic.
        }
    }
}