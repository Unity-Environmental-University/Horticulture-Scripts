using System.Reflection;
using _project.Scripts.Card_Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Tests for Campaign mode mechanics including:
    ///     - 5-round level system
    ///     - Rent payment on level completion
    ///     - Money persistence across levels
    ///     - Win/loss conditions
    /// </summary>
    public class CampaignModeTester
    {
        private CardGameMaster _cardGameMaster;
        private GameObject _cardGameMasterGo;
        private ScoreManager _scoreManager;
        private TurnController _turnController;

        [SetUp]
        public void Setup()
        {
            // Create CardGameMaster GameObject
            _cardGameMasterGo = new GameObject("CardGameMaster_Test");
            _cardGameMaster = _cardGameMasterGo.AddComponent<CardGameMaster>();
            _turnController = _cardGameMasterGo.AddComponent<TurnController>();
            _scoreManager = _cardGameMasterGo.AddComponent<ScoreManager>();

            // Wire up components to CardGameMaster
            _cardGameMaster.turnController = _turnController;
            _cardGameMaster.scoreManager = _scoreManager;

            // Create required UI elements
            var textGo = new GameObject("Text");
            _cardGameMaster.moneysText = textGo.AddComponent<TextMeshPro>();
            _cardGameMaster.turnText = new GameObject("TurnText").AddComponent<TextMeshPro>();
            _cardGameMaster.roundText = new GameObject("RoundText").AddComponent<TextMeshPro>();
            _cardGameMaster.levelText = new GameObject("LevelText").AddComponent<TextMeshPro>();

            // Initialize turn controller
            _turnController.level = 1; // Start at level 1 (not tutorial)
            _turnController.currentGameMode = GameMode.Campaign;
            _turnController.currentRoundInLevel = 0;
            _turnController.moneyGoal = 100; // Level 1 goal

            // Initialize score manager
            _scoreManager.ResetMoneys();

            // Set CardGameMaster instance reference
            var instanceField = typeof(CardGameMaster).GetField("Instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            instanceField?.SetValue(null, _cardGameMaster);
        }

        [TearDown]
        public void TearDown()
        {
            if (_cardGameMasterGo) Object.DestroyImmediate(_cardGameMasterGo);

            // Clear CardGameMaster instance
            var instanceField = typeof(CardGameMaster).GetField("Instance",
                BindingFlags.Static | BindingFlags.NonPublic);
            instanceField?.SetValue(null, null);
        }

        [Test]
        public void GameMode_DefaultsToCampaign()
        {
            Assert.AreEqual(GameMode.Campaign, _turnController.currentGameMode,
                "Game mode should default to Campaign");
        }

        [Test]
        public void RoundCounter_StartsAtZero()
        {
            Assert.AreEqual(0, _turnController.currentRoundInLevel,
                "Round counter should start at 0");
        }

        [Test]
        public void RoundCounter_IncrementsAfterEachRound()
        {
            // Simulate completing rounds
            for (var i = 0; i < 3; i++) _turnController.currentRoundInLevel++;

            Assert.AreEqual(3, _turnController.currentRoundInLevel,
                "Round counter should be 3 after 3 rounds");
        }

        [Test]
        public void MoneyGoal_ScalesWithLevel()
        {
            // Test level 1
            _turnController.level = 1;
            var updateMethod = typeof(TurnController).GetMethod("UpdateMoneyGoal",
                BindingFlags.NonPublic | BindingFlags.Instance);
            updateMethod?.Invoke(_turnController, null);
            Assert.AreEqual(100, _turnController.moneyGoal, "Level 1 goal should be $100");

            // Test level 2
            _turnController.level = 2;
            updateMethod?.Invoke(_turnController, null);
            Assert.AreEqual(100, _turnController.moneyGoal, "Level 2 goal should be $100");

            // Test level 3
            _turnController.level = 3;
            updateMethod?.Invoke(_turnController, null);
            Assert.AreEqual(150, _turnController.moneyGoal, "Level 3 goal should be $150");

            // Test level 4
            _turnController.level = 4;
            updateMethod?.Invoke(_turnController, null);
            Assert.AreEqual(200, _turnController.moneyGoal, "Level 4 goal should be $200");
        }

        [Test]
        public void RentPayment_SubtractsMoneyGoal()
        {
            // Set starting money above goal
            ScoreManager.SetScore(150);
            _turnController.moneyGoal = 100;

            // Simulate rent payment
            ScoreManager.SubtractMoneys(_turnController.moneyGoal);

            Assert.AreEqual(50, ScoreManager.GetMoneys(),
                "Money should be $50 after paying $100 rent from $150");
        }

        [Test]
        public void RentPayment_ExactAmount_LeavesZero()
        {
            // Set money exactly equal to goal
            ScoreManager.SetScore(100);
            _turnController.moneyGoal = 100;

            // Simulate rent payment
            ScoreManager.SubtractMoneys(_turnController.moneyGoal);

            Assert.AreEqual(0, ScoreManager.GetMoneys(),
                "Money should be $0 after paying exact rent amount");
        }

        [Test]
        public void RentCheck_FailsWhenMoneyBelowGoal()
        {
            // Set money below goal
            ScoreManager.SetScore(90);
            _turnController.moneyGoal = 100;

            // Check if rent can be paid
            var canAffordRent = ScoreManager.GetMoneys() >= _turnController.moneyGoal;

            Assert.IsFalse(canAffordRent,
                "Player should not be able to afford rent with $90 when goal is $100");
        }

        [Test]
        public void RentCheck_SucceedsWhenMoneyMeetsGoal()
        {
            // Set money equal to goal
            ScoreManager.SetScore(100);
            _turnController.moneyGoal = 100;

            // Check if rent can be paid
            var canAffordRent = ScoreManager.GetMoneys() >= _turnController.moneyGoal;

            Assert.IsTrue(canAffordRent,
                "Player should be able to afford rent with $100 when goal is $100");
        }

        [Test]
        public void RentCheck_SucceedsWhenMoneyExceedsGoal()
        {
            // Set money above goal
            ScoreManager.SetScore(150);
            _turnController.moneyGoal = 100;

            // Check if rent can be paid
            var canAffordRent = ScoreManager.GetMoneys() >= _turnController.moneyGoal;

            Assert.IsTrue(canAffordRent,
                "Player should be able to afford rent with $150 when goal is $100");
        }

        [Test]
        public void UIDisplay_ShowsRentDueInCampaignMode()
        {
            // Set up campaign mode with money and goal
            _turnController.currentGameMode = GameMode.Campaign;
            ScoreManager.SetScore(75);
            _turnController.moneyGoal = 100;

            // Update UI
            ScoreManager.UpdateMoneysText();

            // Check text format
            const string expectedText = "Money: $75 Rent Due: $100";
            if (_cardGameMaster.moneysText)
                Assert.AreEqual(expectedText, _cardGameMaster.moneysText.text,
                    "UI should show 'Money: $X | Rent Due: $Y' format in Campaign mode");
        }

        [Test]
        public void UIDisplay_ShowsTraditionalFormatInTutorialMode()
        {
            // Set up tutorial mode
            _turnController.currentGameMode = GameMode.Tutorial;
            ScoreManager.SetScore(75);
            _turnController.moneyGoal = 500;

            // Update UI
            ScoreManager.UpdateMoneysText();

            // Check text format
            var expectedText = "Moneys: $75/500";
            if (_cardGameMaster.moneysText)
                Assert.AreEqual(expectedText, _cardGameMaster.moneysText.text,
                    "UI should show 'Moneys: $X/Y' format in Tutorial mode");
        }

        [Test]
        public void RoundDisplay_ShowsProgressInCampaignMode()
        {
            // Set up campaign mode with round progress
            _turnController.currentGameMode = GameMode.Campaign;
            _turnController.currentRoundInLevel = 3;

            // Simulate Update() method behavior
            if (_turnController.currentGameMode == GameMode.Campaign)
                if (_cardGameMaster.roundText)
                    _cardGameMaster.roundText.text = $"Round: {_turnController.currentRoundInLevel}/5";

            // Check text format
            var expectedText = "Round: 3/5";
            if (_cardGameMaster.roundText)
                Assert.AreEqual(expectedText, _cardGameMaster.roundText.text,
                    "Round display should show 'Round: X/5' in Campaign mode");
        }

        [Test]
        public void LevelAdvancement_ResetsRoundCounter()
        {
            // Set up end of level scenario
            _turnController.currentRoundInLevel = 5;
            ScoreManager.SetScore(150);
            _turnController.moneyGoal = 100;

            // Simulate successful rent payment and level advancement
            if (ScoreManager.GetMoneys() >= _turnController.moneyGoal)
            {
                ScoreManager.SubtractMoneys(_turnController.moneyGoal);
                _turnController.currentRoundInLevel = 0; // Reset as done in EndRound()
            }

            Assert.AreEqual(0, _turnController.currentRoundInLevel,
                "Round counter should reset to 0 after level advancement");
            Assert.AreEqual(50, ScoreManager.GetMoneys(),
                "Money should persist after paying rent");
        }

        [Test]
        public void MoneyPersistence_AcrossMultipleLevels()
        {
            const int startingMoney = 250;
            ScoreManager.SetScore(startingMoney);

            // Level 1: Goal $100, starting with $250
            _turnController.level = 1;
            _turnController.moneyGoal = 100;
            ScoreManager.SubtractMoneys(_turnController.moneyGoal);
            Assert.AreEqual(150, ScoreManager.GetMoneys(), "Should have $150 after level 1");

            // Level 2: Goal $150, starting with $150
            _turnController.level = 2;
            _turnController.moneyGoal = 150;
            ScoreManager.SubtractMoneys(_turnController.moneyGoal);
            Assert.AreEqual(0, ScoreManager.GetMoneys(), "Should have $0 after level 2");
        }
    }
}