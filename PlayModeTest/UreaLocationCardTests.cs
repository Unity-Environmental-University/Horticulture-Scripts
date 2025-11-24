using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.GameState;
using NUnit.Framework;
using UnityEngine;

namespace _project.Scripts.PlayModeTest
{
    public class UreaLocationCardTests
    {
        [Test]
        public void UreaBasic_FirstApplication_DoublesPlantValue()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard { Value = 10 };
            plantController.PlantCard = plantCard;
            var urea = new UreaBasic();

            // Act
            urea.ApplyLocationEffect(plantController);

            // Assert
            Assert.AreEqual(20, plantCard.Value, "First Urea application should double plant value from 10 to 20");
            Assert.AreEqual(10, plantCard.BaseValue, "BaseValue should be stored as original value (10)");
            Assert.AreEqual(1, plantController.uLocationCards.Count, "uLocationCards should contain one entry");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void UreaBasic_SecondApplication_AddsHalfOfBaseValue()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard { Value = 10 };
            plantController.PlantCard = plantCard;
            var urea = new UreaBasic();

            // Act - First application
            urea.ApplyLocationEffect(plantController);
            // Value should now be 20, BaseValue = 10

            // Act - Second application
            urea.ApplyLocationEffect(plantController);

            // Assert
            // Second use should add 50% of base (10 * 0.5 = 5)
            // Expected: 20 + 5 = 25
            Assert.AreEqual(25, plantCard.Value, "Second Urea application should add 50% of base value (20 + 5 = 25)");
            Assert.AreEqual(10, plantCard.BaseValue, "BaseValue should remain at original value (10)");
            Assert.AreEqual(2, plantController.uLocationCards.Count, "uLocationCards should contain two entries");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void UreaBasic_ThirdApplication_AddsThirdOfBaseValue()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard { Value = 10 };
            plantController.PlantCard = plantCard;
            var urea = new UreaBasic();

            // Act - Three applications
            urea.ApplyLocationEffect(plantController); // 10 -> 20
            urea.ApplyLocationEffect(plantController); // 20 -> 25
            urea.ApplyLocationEffect(plantController); // 25 -> ?

            // Assert
            // Third use should add 33% of base (10 * 0.333... = 3.33, int cast = 3)
            // Expected: 25 + 3 = 28
            Assert.AreEqual(28, plantCard.Value, "Third Urea application should add ~33% of base value (25 + 3 = 28)");
            Assert.AreEqual(10, plantCard.BaseValue, "BaseValue should remain at original value (10)");
            Assert.AreEqual(3, plantController.uLocationCards.Count, "uLocationCards should contain three entries");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void UreaBasic_FourthApplication_AddsQuarterOfBaseValue()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard { Value = 10 };
            plantController.PlantCard = plantCard;
            var urea = new UreaBasic();

            // Act - Four applications
            urea.ApplyLocationEffect(plantController); // 10 -> 20
            urea.ApplyLocationEffect(plantController); // 20 -> 25
            urea.ApplyLocationEffect(plantController); // 25 -> 28
            urea.ApplyLocationEffect(plantController); // 28 -> ?

            // Assert
            // Fourth use should add 25% of base (10 * 0.25 = 2.5, int cast = 2)
            // Expected: 28 + 2 = 30
            Assert.AreEqual(30, plantCard.Value, "Fourth Urea application should add 25% of base value (28 + 2 = 30)");
            Assert.AreEqual(10, plantCard.BaseValue, "BaseValue should remain at original value (10)");
            Assert.AreEqual(4, plantController.uLocationCards.Count, "uLocationCards should contain four entries");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void UreaBasic_MaxValueCap_RespectedAtBaseValueSquared()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard { Value = 10 };
            plantController.PlantCard = plantCard;
            var urea = new UreaBasic();

            // Act - First application to set BaseValue
            urea.ApplyLocationEffect(plantController);
            Assert.AreEqual(10, plantCard.BaseValue, "BaseValue should be set to 10");
            Assert.AreEqual(20, plantCard.Value, "Value should be 20 after first application");

            // Act - Manually set value close to cap to test cap mechanism
            plantCard.Value = 97;
            // Add to uLocationCards to simulate one previous application
            plantController.uLocationCards.Add("UreaBasic");

            // Act - Apply Urea again (second application from tracker's perspective)
            // Would try: 97 + round(10 * 0.5) = 97 + 5 = 102
            // But cap is 10 * 10 = 100, so should be capped
            urea.ApplyLocationEffect(plantController);

            // Assert
            Assert.AreEqual(100, plantCard.Value, "Plant value should cap at BaseValue squared (10 * 10 = 100)");
            Assert.AreEqual(10, plantCard.BaseValue, "BaseValue should remain at original value (10)");
            Assert.AreEqual(3, plantController.uLocationCards.Count, "Should have 3 location card entries");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void UreaBasic_MultiplePlants_TrackIndependentBaseValues()
        {
            // Arrange
            var gameObject1 = new GameObject("TestPlant1");
            var plantController1 = gameObject1.AddComponent<PlantController>();
            var plantCard1 = new ColeusCard { Value = 5 };
            plantController1.PlantCard = plantCard1;

            var gameObject2 = new GameObject("TestPlant2");
            var plantController2 = gameObject2.AddComponent<PlantController>();
            var plantCard2 = new ChrysanthemumCard { Value = 8 };
            plantController2.PlantCard = plantCard2;

            var urea = new UreaBasic();

            // Act - Apply to both plants
            urea.ApplyLocationEffect(plantController1); // 5 -> 10
            urea.ApplyLocationEffect(plantController2); // 8 -> 16

            // Assert
            Assert.AreEqual(10, plantCard1.Value, "Plant 1 value should be doubled (5 -> 10)");
            Assert.AreEqual(5, plantCard1.BaseValue, "Plant 1 BaseValue should be 5");

            Assert.AreEqual(16, plantCard2.Value, "Plant 2 value should be doubled (8 -> 16)");
            Assert.AreEqual(8, plantCard2.BaseValue, "Plant 2 BaseValue should be 8");

            // Act - Apply second time to plant 1
            urea.ApplyLocationEffect(plantController1); // 10 -> 12 (10 + 5*0.5)

            // Assert
            Assert.AreEqual(12, plantCard1.Value, "Plant 1 second application should add 50% of base (10 + 2.5 = 12)");
            Assert.AreEqual(16, plantCard2.Value, "Plant 2 value should remain unchanged");

            Object.DestroyImmediate(gameObject1);
            Object.DestroyImmediate(gameObject2);
        }

        [Test]
        public void UreaBasic_DifferentPlantTypes_AllSupportDiminishingReturns()
        {
            // Arrange & Act & Assert for each plant type

            // Coleus (value = 5)
            var coleusObject = new GameObject("Coleus");
            var coleusController = coleusObject.AddComponent<PlantController>();
            var coleusCard = new ColeusCard { Value = 5 };
            coleusController.PlantCard = coleusCard;
            var urea1 = new UreaBasic();

            urea1.ApplyLocationEffect(coleusController);
            Assert.AreEqual(10, coleusCard.Value, "Coleus: First use doubles (5 -> 10)");
            Assert.AreEqual(5, coleusCard.BaseValue, "Coleus: BaseValue is 5");

            // Chrysanthemum (value = 8)
            var chrysObject = new GameObject("Chrysanthemum");
            var chrysController = chrysObject.AddComponent<PlantController>();
            var chrysCard = new ChrysanthemumCard { Value = 8 };
            chrysController.PlantCard = chrysCard;
            var urea2 = new UreaBasic();

            urea2.ApplyLocationEffect(chrysController);
            Assert.AreEqual(16, chrysCard.Value, "Chrysanthemum: First use doubles (8 -> 16)");
            Assert.AreEqual(8, chrysCard.BaseValue, "Chrysanthemum: BaseValue is 8");

            // Pepper (value = 4)
            var pepperObject = new GameObject("Pepper");
            var pepperController = pepperObject.AddComponent<PlantController>();
            var pepperCard = new PepperCard { Value = 4 };
            pepperController.PlantCard = pepperCard;
            var urea3 = new UreaBasic();

            urea3.ApplyLocationEffect(pepperController);
            Assert.AreEqual(8, pepperCard.Value, "Pepper: First use doubles (4 -> 8)");
            Assert.AreEqual(4, pepperCard.BaseValue, "Pepper: BaseValue is 4");

            // Cucumber (value = 3)
            var cucumberObject = new GameObject("Cucumber");
            var cucumberController = cucumberObject.AddComponent<PlantController>();
            var cucumberCard = new CucumberCard { Value = 3 };
            cucumberController.PlantCard = cucumberCard;
            var urea4 = new UreaBasic();

            urea4.ApplyLocationEffect(cucumberController);
            Assert.AreEqual(6, cucumberCard.Value, "Cucumber: First use doubles (3 -> 6)");
            Assert.AreEqual(3, cucumberCard.BaseValue, "Cucumber: BaseValue is 3");

            Object.DestroyImmediate(coleusObject);
            Object.DestroyImmediate(chrysObject);
            Object.DestroyImmediate(pepperObject);
            Object.DestroyImmediate(cucumberObject);
        }

        [Test]
        public void UreaBasic_BaseValueStoredOnlyOnFirstUse()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard { Value = 10 };
            plantController.PlantCard = plantCard;
            var urea = new UreaBasic();

            // Act - First application
            urea.ApplyLocationEffect(plantController);

            // Assert after first use
            Assert.AreEqual(10, plantCard.BaseValue, "BaseValue should be set to 10 on first use");

            // Act - Manually change Value to simulate other effects
            plantCard.Value = 30;

            // Act - Second application
            urea.ApplyLocationEffect(plantController);

            // Assert
            // BaseValue should still be 10, not 30
            // Boost should be 10 * 0.5 = 5
            // Expected: 30 + 5 = 35
            Assert.AreEqual(10, plantCard.BaseValue, "BaseValue should remain 10, not change to 30");
            Assert.AreEqual(35, plantCard.Value, "Second use should use original BaseValue (30 + 5 = 35)");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void UreaBasic_SaveLoad_PreservesBaseValue()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard { Value = 10 };
            plantController.PlantCard = plantCard;
            var urea = new UreaBasic();

            // Act - Apply Urea twice
            urea.ApplyLocationEffect(plantController); // 10 -> 20, BaseValue = 10
            urea.ApplyLocationEffect(plantController); // 20 -> 25

            // Assert initial state
            Assert.AreEqual(10, plantCard.BaseValue, "BaseValue should be 10 before save");
            Assert.AreEqual(25, plantCard.Value, "Value should be 25 before save");

            // Act - Serialize the card (simulating save)
            var cardData = new CardData
            {
                cardTypeName = plantCard.GetType().Name,
                value = plantCard.Value,
                baseValue = plantCard.BaseValue
            };

            // Act - Deserialize the card (simulating load)
            var loadedCard = GameStateManager.DeserializeCard(cardData) as ColeusCard;

            // Assert loaded card preserves both Value and BaseValue
            Assert.IsNotNull(loadedCard, "Loaded card should not be null");
            Assert.AreEqual(25, loadedCard.Value, "Loaded card Value should be 25");
            Assert.AreEqual(10, loadedCard.BaseValue, "Loaded card BaseValue should be 10 (preserved from save)");

            // Act - Apply Urea to loaded card
            var loadedGameObject = new GameObject("LoadedPlant");
            var loadedController = loadedGameObject.AddComponent<PlantController>();
            loadedController.PlantCard = loadedCard;
            // Simulate that Urea was already applied twice (must restore uLocationCards list)
            loadedController.uLocationCards.Add("UreaBasic");
            loadedController.uLocationCards.Add("UreaBasic");

            urea.ApplyLocationEffect(loadedController); // Third application

            // Assert - Third application should use saved BaseValue (10), not default (5)
            // Third use should add ~33% of BaseValue: 25 + round(10 * 0.333) = 25 + 3 = 28
            Assert.AreEqual(28, loadedCard.Value, "Third Urea after load should use saved BaseValue (25 + 3 = 28)");
            Assert.AreEqual(10, loadedCard.BaseValue, "BaseValue should remain 10 after load and third application");

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(loadedGameObject);
        }
    }
}