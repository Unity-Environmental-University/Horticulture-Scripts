using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Stickers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable PossibleInvalidOperationException

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Tests for the per-level plant price boost feature in DeckManager.
    ///     Validates category selection, boost amount generation, and price application logic.
    /// </summary>
    public class PlantPriceBoostTests
    {
        // Reflection field info for accessing private static fields
        private FieldInfo _boostedCategoryField;
        private DeckManager _deckManager;
        private FieldInfo _plantDeckField;
        private FieldInfo _priceBoostAmountField;
        private GameObject _testGameObject;

        [SetUp]
        public void Setup()
        {
            LogAssert.ignoreFailingMessages = true;

            // Create minimal DeckManager setup
            _testGameObject = new GameObject("TestDeckManager");
            _deckManager = _testGameObject.AddComponent<DeckManager>();

            // Get reflection access to private static fields
            var deckManagerType = typeof(DeckManager);
            _boostedCategoryField =
                deckManagerType.GetField("_boostedCategory", BindingFlags.NonPublic | BindingFlags.Static);
            _priceBoostAmountField =
                deckManagerType.GetField("_priceBoostAmount", BindingFlags.NonPublic | BindingFlags.Static);
            _plantDeckField = deckManagerType.GetField("PlantDeck", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(_boostedCategoryField, "Could not access _boostedCategory field via reflection");
            Assert.IsNotNull(_priceBoostAmountField, "Could not access _priceBoostAmount field via reflection");
            Assert.IsNotNull(_plantDeckField, "Could not access PlantDeck field via reflection");

            // Reset static state
            ResetStaticState();
        }

        [TearDown]
        public void TearDown()
        {
            ResetStaticState();
            Object.Destroy(_testGameObject);
        }

        private void ResetStaticState()
        {
            _boostedCategoryField?.SetValue(null, null);
            _priceBoostAmountField?.SetValue(null, 0);

            if (_plantDeckField?.GetValue(null) is List<ICard> plantDeck) plantDeck.Clear();
        }

        private PlantCardCategory? GetBoostedCategory()
        {
            return (PlantCardCategory?)_boostedCategoryField.GetValue(null);
        }

        private void SetBoostedCategory(PlantCardCategory? category)
        {
            _boostedCategoryField.SetValue(null, category);
        }

        private int GetPriceBoostAmount()
        {
            return (int)_priceBoostAmountField.GetValue(null);
        }

        private void SetPriceBoostAmount(int amount)
        {
            _priceBoostAmountField.SetValue(null, amount);
        }

        private List<ICard> GetPlantDeck()
        {
            return (List<ICard>)_plantDeckField.GetValue(null);
        }

        private void CallApplyStoredPriceBoost()
        {
            var method = typeof(DeckManager).GetMethod("ApplyStoredPriceBoost",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "Could not access ApplyStoredPriceBoost method via reflection");

            try
            {
                method.Invoke(_deckManager, null);
            }
            catch (TargetInvocationException ex)
            {
                // Unwrap reflection exception to show actual error
                throw ex.InnerException ?? ex;
            }
        }

        private int CallCalculatePriceBoostModifier(int? level = null)
        {
            var method = typeof(DeckManager).GetMethod("CalculatePriceBoostModifier",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Could not access CalculatePriceBoostModifier method via reflection");

            try
            {
                return (int)method.Invoke(null, new object[] { level });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }

        [Test]
        public void CalculatePriceBoostModifier_Level1_Returns1x()
        {
            var modifier = CallCalculatePriceBoostModifier(1);
            Assert.AreEqual(1, modifier, "Level 1 should have 1x multiplier");
        }

        [Test]
        public void CalculatePriceBoostModifier_Level2_Returns1x()
        {
            var modifier = CallCalculatePriceBoostModifier(2);
            Assert.AreEqual(1, modifier, "Level 2 should have 1x multiplier");
        }

        [Test]
        public void CalculatePriceBoostModifier_Level3_Returns2x()
        {
            var modifier = CallCalculatePriceBoostModifier(3);
            Assert.AreEqual(2, modifier, "Level 3 should have 2x multiplier");
        }

        [Test]
        public void CalculatePriceBoostModifier_Level4_Returns2x()
        {
            var modifier = CallCalculatePriceBoostModifier(4);
            Assert.AreEqual(2, modifier, "Level 4 should have 2x multiplier");
        }

        [Test]
        public void CalculatePriceBoostModifier_Level5_Returns3x()
        {
            var modifier = CallCalculatePriceBoostModifier(5);
            Assert.AreEqual(3, modifier, "Level 5 should have 3x multiplier");
        }

        [Test]
        public void CalculatePriceBoostModifier_Level10_Returns5x()
        {
            var modifier = CallCalculatePriceBoostModifier(10);
            Assert.AreEqual(5, modifier, "Level 10 should have 5x multiplier");
        }

        [Test]
        public void CalculatePriceBoostModifier_Level20_Returns10x()
        {
            var modifier = CallCalculatePriceBoostModifier(20);
            Assert.AreEqual(10, modifier, "Level 20 should have 10x multiplier");
        }

        [Test]
        public void CalculatePriceBoostModifier_Level50_CappedAt20x()
        {
            var modifier = CallCalculatePriceBoostModifier(50);
            Assert.AreEqual(20, modifier, "Level 50 should be capped at 20x multiplier");
        }

        [Test]
        public void CalculatePriceBoostModifier_Level100_CappedAt20x()
        {
            var modifier = CallCalculatePriceBoostModifier(100);
            Assert.AreEqual(20, modifier, "Level 100 should be capped at 20x multiplier");
        }

        [Test]
        public void GeneratePlantPrices_ScalesBoostWithLevel()
        {
            // Test that boost amount increases at higher levels
            DeckManager.GeneratePlantPrices(1);
            var level1Boost = GetPriceBoostAmount();
            Assert.GreaterOrEqual(level1Boost, 2, "Level 1 boost should be at least 2 (2*1)");
            Assert.LessOrEqual(level1Boost, 4, "Level 1 boost should be at most 4 (4*1)");

            DeckManager.GeneratePlantPrices(5);
            var level5Boost = GetPriceBoostAmount();
            Assert.GreaterOrEqual(level5Boost, 6, "Level 5 boost should be at least 6 (2*3)");
            Assert.LessOrEqual(level5Boost, 12, "Level 5 boost should be at most 12 (4*3)");

            DeckManager.GeneratePlantPrices(10);
            var level10Boost = GetPriceBoostAmount();
            Assert.GreaterOrEqual(level10Boost, 10, "Level 10 boost should be at least 10 (2*5)");
            Assert.LessOrEqual(level10Boost, 20, "Level 10 boost should be at most 20 (4*5)");
        }

        [Test]
        public void GeneratePlantPrices_SelectsValidCategory()
        {
            // Test multiple times to ensure randomness stays within a valid range
            for (var i = 0; i < 25; i++)
            {
                DeckManager.GeneratePlantPrices();

                var category = GetBoostedCategory();
                Assert.IsNotNull(category, $"Iteration {i}: Category should not be null after GeneratePlantPrices");
                Assert.IsTrue(
                    category == PlantCardCategory.Fruiting || category == PlantCardCategory.Decorative,
                    $"Iteration {i}: Category {category} should be Fruiting or Decorative, not Other"
                );
            }
        }

        [Test]
        public void GeneratePlantPrices_SelectsBothCategories()
        {
            // Collect all generated categories to verify distribution
            var categories = new HashSet<PlantCardCategory?>();

            // Run enough iterations to ensure both categories appear (probabilistic test)
            for (var i = 0; i < 50; i++)
            {
                DeckManager.GeneratePlantPrices();
                categories.Add(GetBoostedCategory());
            }

            // Verify both valid categories appear at least once
            Assert.IsTrue(categories.Contains(PlantCardCategory.Fruiting),
                "Should generate Fruiting category at least once in 50 iterations");
            Assert.IsTrue(categories.Contains(PlantCardCategory.Decorative),
                "Should generate Decorative category at least once in 50 iterations");

            // Verify Other is never selected
            Assert.IsFalse(categories.Contains(PlantCardCategory.Other),
                "Should never generate Other category");
        }

        [Test]
        public void GeneratePlantPrices_SelectsValidBoostAmount()
        {
            // Collect all boost amounts to verify range coverage
            var boostAmounts = new HashSet<int>();

            for (var i = 0; i < 30; i++)
            {
                DeckManager.GeneratePlantPrices();

                var amount = GetPriceBoostAmount();
                boostAmounts.Add(amount);

                Assert.GreaterOrEqual(amount, 2, $"Iteration {i}: Boost amount {amount} should be >= 2");
                Assert.LessOrEqual(amount, 4, $"Iteration {i}: Boost amount {amount} should be <= 4");
            }

            // With 30 iterations, we should see multiple different values (2, 3, or 4)
            Assert.GreaterOrEqual(boostAmounts.Count, 2,
                "Should generate at least 2 different boost amounts across 30 iterations");
        }

        [Test]
        public void ApplyStoredPriceBoost_IncreasesMatchingCardValues()
        {
            // Setup: Boost Fruiting plants by 3
            SetBoostedCategory(PlantCardCategory.Fruiting);
            const int boostAmount = 3;
            SetPriceBoostAmount(boostAmount);

            // Add a Fruiting plant
            var pepperCard = new PepperCard();
            var initialValue = pepperCard.Value.Value;

            var plantDeck = GetPlantDeck();
            plantDeck.Add(pepperCard);

            // Apply boost
            CallApplyStoredPriceBoost();

            // Verify boost was applied
            Assert.AreEqual(initialValue + boostAmount, pepperCard.Value,
                $"PepperCard value should increase from {initialValue} to {initialValue + boostAmount} (boost of {boostAmount})");
        }

        [Test]
        public void ApplyStoredPriceBoost_DoesNotAffectNonMatchingCategories()
        {
            // Setup: Boost Fruiting plants by 3
            SetBoostedCategory(PlantCardCategory.Fruiting);
            SetPriceBoostAmount(3);

            // Add a Decorative plant (should NOT be boosted)
            var coleusCard = new ColeusCard();
            var initialValue = coleusCard.Value.Value;

            var plantDeck = GetPlantDeck();
            plantDeck.Add(coleusCard);

            // Apply boost
            CallApplyStoredPriceBoost();

            // Verify Decorative card was NOT boosted
            Assert.AreEqual(initialValue, coleusCard.Value,
                $"ColeusCard value should remain {initialValue} (not boosted)");
        }

        [Test]
        public void ApplyStoredPriceBoost_WithNoBoostedCategory_DoesNothing()
        {
            // Setup: No boosted category (null)
            SetBoostedCategory(null);
            SetPriceBoostAmount(5);

            // Add cards to the deck
            var pepperCard = new PepperCard();
            var coleusCard = new ColeusCard();
            var initialPepperValue = pepperCard.Value;
            var initialColeusValue = coleusCard.Value;

            var plantDeck = GetPlantDeck();
            plantDeck.Add(pepperCard);
            plantDeck.Add(coleusCard);

            // Apply boost (should do nothing)
            CallApplyStoredPriceBoost();

            // Verify no values changed
            Assert.AreEqual(initialPepperValue, pepperCard.Value,
                "PepperCard value should not change when category is null");
            Assert.AreEqual(initialColeusValue, coleusCard.Value,
                "ColeusCard value should not change when category is null");
        }

        [Test]
        public void ApplyStoredPriceBoost_AppliesCorrectAmountToMultipleCards()
        {
            // Setup: Boost Decorative plants by 4
            SetBoostedCategory(PlantCardCategory.Decorative);
            const int boostAmount = 4;
            SetPriceBoostAmount(boostAmount);

            // Add multiple Decorative plants
            var coleusCard = new ColeusCard();
            var chrysanthemumCard = new ChrysanthemumCard();
            var coleusInitialValue = coleusCard.Value.Value;
            var chrysanthemumInitialValue = chrysanthemumCard.Value.Value;

            var plantDeck = GetPlantDeck();
            plantDeck.Add(coleusCard);
            plantDeck.Add(chrysanthemumCard);

            // Apply boost
            CallApplyStoredPriceBoost();

            // Verify both cards received the same boost amount
            Assert.AreEqual(coleusInitialValue + boostAmount, coleusCard.Value,
                $"ColeusCard should increase from {coleusInitialValue} to {coleusInitialValue + boostAmount}");
            Assert.AreEqual(chrysanthemumInitialValue + boostAmount, chrysanthemumCard.Value,
                $"ChrysanthemumCard should increase from {chrysanthemumInitialValue} to {chrysanthemumInitialValue + boostAmount}");
        }

        [Test]
        public void ApplyStoredPriceBoost_MixedDeck_OnlyBoostsMatchingCategory()
        {
            // Setup: Boost Fruiting by 2
            SetBoostedCategory(PlantCardCategory.Fruiting);
            const int boostAmount = 2;
            SetPriceBoostAmount(boostAmount);

            // Add mixed category deck
            var pepperCard = new PepperCard();
            var cucumberCard = new CucumberCard();
            var coleusCard = new ColeusCard();
            var chrysanthemumCard = new ChrysanthemumCard();

            var pepperInitial = pepperCard.Value.Value;
            var cucumberInitial = cucumberCard.Value.Value;
            var coleusInitial = coleusCard.Value.Value;
            var chrysanthemumInitial = chrysanthemumCard.Value.Value;

            var plantDeck = GetPlantDeck();
            plantDeck.Add(pepperCard);
            plantDeck.Add(cucumberCard);
            plantDeck.Add(coleusCard);
            plantDeck.Add(chrysanthemumCard);

            // Apply boost
            CallApplyStoredPriceBoost();

            // Verify only Fruiting cards were boosted
            Assert.AreEqual(pepperInitial + boostAmount, pepperCard.Value,
                $"PepperCard (Fruiting) should be boosted from {pepperInitial} to {pepperInitial + boostAmount}");
            Assert.AreEqual(cucumberInitial + boostAmount, cucumberCard.Value,
                $"CucumberCard (Fruiting) should be boosted from {cucumberInitial} to {cucumberInitial + boostAmount}");
            Assert.AreEqual(coleusInitial, coleusCard.Value,
                $"ColeusCard (Decorative) should remain {coleusInitial}");
            Assert.AreEqual(chrysanthemumInitial, chrysanthemumCard.Value,
                $"ChrysanthemumCard (Decorative) should remain {chrysanthemumInitial}");
        }

        [Test]
        public void ApplyStoredPriceBoost_WithNullValueCard_HandlesGracefully()
        {
            // Setup: Boost Fruiting by 3
            SetBoostedCategory(PlantCardCategory.Fruiting);
            SetPriceBoostAmount(3);

            // Create a test card with a null value
            var testCard = new TestPlantCardWithNullValue();
            Assert.IsNull(testCard.Value, "Test card should have null value");

            var plantDeck = GetPlantDeck();
            plantDeck.Add(testCard);

            // Apply boost should not throw an exception
            Assert.DoesNotThrow(CallApplyStoredPriceBoost, "Should handle null Value gracefully");

            // Verify card still has null value (no boost applied)
            Assert.IsNull(testCard.Value, "Null value should remain null after boost attempt");
        }

        /// <summary>
        ///     Test implementation of IPlantCard with null Value for testing edge cases.
        /// </summary>
        private class TestPlantCardWithNullValue : IPlantCard
        {
            public string Name => "Test Plant";
            public string Description => "Test Description";
            public int? Value { get; set; }
            public PlantCardCategory Category => PlantCardCategory.Fruiting;

            public int BaseValue { get; set; }
            public InfectLevel Infect { get; } = new();

            public int EggLevel
            {
                get => Infect.EggTotal;
                set => Infect.SetEggs("Manual", value);
            }

            public GameObject Prefab => null;
            public List<ISticker> Stickers { get; } = new();

            public ICard Clone()
            {
                return new TestPlantCardWithNullValue { Value = Value };
            }
        }
    }
}