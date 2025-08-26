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
            Assert.AreEqual(0, coleusCard.Infect.InfectTotal, "Infect total should default to 0");
            
            // Act
            coleusCard.Infect.SetInfect("Test", 5);
            
            // Assert
            Assert.AreEqual(5, coleusCard.Infect.InfectTotal, "Infect total should be settable");
            
            // Arrange
            var chrysanthemumCard = new ChrysanthemumCard();
            
            // Act
            chrysanthemumCard.Infect.SetInfect("Test", 3);
            
            // Assert
            Assert.AreEqual(3, chrysanthemumCard.Infect.InfectTotal, "Chrysanthemum infect total should be settable");
            
            var pepperCard = new PepperCard();
            pepperCard.Infect.SetInfect("Test", 7);
            Assert.AreEqual(7, pepperCard.Infect.InfectTotal, "Pepper infect total should be settable");
            
            var cucumberCard = new CucumberCard();
            cucumberCard.Infect.SetInfect("Test", 2);
            Assert.AreEqual(2, cucumberCard.Infect.InfectTotal, "Cucumber infect total should be settable");
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
            originalCard.Infect.SetInfect("Manual", 4);
            originalCard.EggLevel = 2;
            originalCard.Value = 8;
            
            // Act
            var clonedCard = (ColeusCard)originalCard.Clone();
            
            // Assert
            Assert.AreEqual(4, clonedCard.Infect.InfectTotal, "Cloned card should preserve infect total");
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
            Assert.AreEqual(0, plantController.GetInfectLevel(), "PlantController infect total should default to 0");
            
            // Act
            plantController.SetInfectLevel(6);
            
            // Assert
            Assert.AreEqual(6, plantController.GetInfectLevel(), "PlantController infect total should be settable");
            Assert.AreEqual(6, plantCard.Infect.InfectTotal, "Setting PlantController infect total should update card");
            
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
            plantController.SetInfectLevel(-5);
            
            // Assert
            Assert.AreEqual(0, plantController.GetInfectLevel(), "Negative infect total should be clamped to 0");
            
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
            coleusCard.Infect.SetInfect("Test", -10);
            
            // Assert
            Assert.AreEqual(0, coleusCard.Infect.InfectTotal, "Negative infect total should be clamped to 0 at card level");
            
            // Act
            coleusCard.EggLevel = -5;
            
            // Assert
            Assert.AreEqual(0, coleusCard.EggLevel, "Negative EggLevel should be clamped to 0 at card level");
        }
    }
}
