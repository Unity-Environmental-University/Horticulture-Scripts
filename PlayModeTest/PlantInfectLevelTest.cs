using _project.Scripts.Classes;
using _project.Scripts.Core;
using NUnit.Framework;
using UnityEngine;

namespace _project.Scripts.PlayModeTest
{
    public class PlantInfectLevelTest
    {
        [Test]
        public void PlantCard_InfectLevel_CanBeSetAndRetrieved()
        {
            // Arrange
            var coleusCard = new ColeusCard();
            
            // Assert
            Assert.AreEqual(0, coleusCard.InfectLevel, "InfectLevel should default to 0");
            
            // Act
            coleusCard.InfectLevel = 5;
            
            // Assert
            Assert.AreEqual(5, coleusCard.InfectLevel, "InfectLevel should be settable");
            
            // Arrange
            var chrysanthemumCard = new ChrysanthemumCard();
            
            // Act
            chrysanthemumCard.InfectLevel = 3;
            
            // Assert
            Assert.AreEqual(3, chrysanthemumCard.InfectLevel, "Chrysanthemum InfectLevel should be settable");
            
            var pepperCard = new PepperCard();
            pepperCard.InfectLevel = 7;
            Assert.AreEqual(7, pepperCard.InfectLevel, "Pepper InfectLevel should be settable");
            
            var cucumberCard = new CucumberCard();
            cucumberCard.InfectLevel = 2;
            Assert.AreEqual(2, cucumberCard.InfectLevel, "Cucumber InfectLevel should be settable");
        }

        [Test]
        public void PlantCard_EggLevel_CanBeSetAndRetrieved()
        {
            // Arrange
            var coleusCard = new ColeusCard();
            
            // Assert
            Assert.AreEqual(0, coleusCard.EggLevel, "EggLevel should default to 0");
            
            // Act
            coleusCard.EggLevel = 3;
            
            // Assert
            Assert.AreEqual(3, coleusCard.EggLevel, "EggLevel should be settable");
        }

        [Test]
        public void PlantCard_Clone_PreservesInfectAndEggLevels()
        {
            // Arrange
            var originalCard = new ColeusCard();
            originalCard.InfectLevel = 4;
            originalCard.EggLevel = 2;
            originalCard.Value = 8;
            
            // Act
            var clonedCard = (ColeusCard)originalCard.Clone();
            
            // Assert
            Assert.AreEqual(4, clonedCard.InfectLevel, "Cloned card should preserve InfectLevel");
            Assert.AreEqual(2, clonedCard.EggLevel, "Cloned card should preserve EggLevel");
            Assert.AreEqual(8, clonedCard.Value, "Cloned card should preserve Value");
        }

        [Test]
        public void PlantController_InfectLevel_PropertyAccess()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard();
            plantController.PlantCard = plantCard;

            // Assert
            Assert.AreEqual(0, plantController.InfectLevel, "PlantController InfectLevel should default to 0");
            
            // Act
            plantController.InfectLevel = 6;
            
            // Assert
            Assert.AreEqual(6, plantController.InfectLevel, "PlantController InfectLevel should be settable");
            Assert.AreEqual(6, plantCard.InfectLevel, "Setting PlantController InfectLevel should update card");
            
            // Act
            plantController.EggLevel = 4;
            
            // Assert
            Assert.AreEqual(4, plantController.EggLevel, "PlantController EggLevel should be settable");
            Assert.AreEqual(4, plantCard.EggLevel, "Setting PlantController EggLevel should update card");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void PlantController_InfectLevel_NegativeValuesClampedToZero()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard();
            plantController.PlantCard = plantCard;

            // Act
            plantController.InfectLevel = -5;
            
            // Assert
            Assert.AreEqual(0, plantController.InfectLevel, "Negative InfectLevel should be clamped to 0");
            
            // Act
            plantController.EggLevel = -3;
            
            // Assert
            Assert.AreEqual(0, plantController.EggLevel, "Negative EggLevel should be clamped to 0");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void PlantCard_InfectLevel_NegativeValuesClampedToZero()
        {
            // Arrange
            var coleusCard = new ColeusCard();
            
            // Act
            coleusCard.InfectLevel = -10;
            
            // Assert
            Assert.AreEqual(0, coleusCard.InfectLevel, "Negative InfectLevel should be clamped to 0 at card level");
            
            // Act
            coleusCard.EggLevel = -5;
            
            // Assert
            Assert.AreEqual(0, coleusCard.EggLevel, "Negative EggLevel should be clamped to 0 at card level");
        }
    }
}