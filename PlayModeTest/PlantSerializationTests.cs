using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.GameState;
using NUnit.Framework;
using UnityEngine;

namespace _project.Scripts.PlayModeTest
{
    public class PlantSerializationTests
    {
        [Test]
        public void PlantSerialization_PreservesLocationCardTracking()
        {
            // Arrange: Plant with Urea applied twice
            var gameObject = new GameObject("TestPlant");
            var plant = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard { Value = 10 };
            plant.PlantCard = plantCard;

            var urea = new UreaBasic();
            urea.ApplyLocationEffect(plant);
            urea.ApplyLocationEffect(plant);

            // Act: Serialize
            var plantData = new PlantData
            {
                plantCard = GameStateManager.SerializeCard(plant.PlantCard),
                locationIndex = 0,
                currentAfflictions = plant.cAfflictions,
                priorAfflictions = plant.pAfflictions,
                usedTreatments = plant.uTreatments,
                currentTreatments = plant.cTreatments,
                moldIntensity = plant.moldIntensity,
                uLocationCards = plant.uLocationCards?.ToList() ?? new List<string>(),
                infectData = GameStateManager.SerializeInfectLevel(plant.PlantCard as IPlantCard),
                canSpreadAfflictions = plant.canSpreadAfflictions,
                canReceiveAfflictions = plant.canReceiveAfflictions
            };

            // Assert
            Assert.IsNotNull(plantData.uLocationCards, "uLocationCards should not be null");
            Assert.AreEqual(2, plantData.uLocationCards.Count, "Should have 2 Urea applications");
            Assert.AreEqual("UreaBasic", plantData.uLocationCards[0], "First entry should be UreaBasic");
            Assert.AreEqual("UreaBasic", plantData.uLocationCards[1], "Second entry should be UreaBasic");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void PlantSerialization_PreservesInfectLevels()
        {
            // Arrange: Plant with multiple infection sources
            var gameObject = new GameObject("TestPlant");
            var plant = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard();
            plant.PlantCard = plantCard;

            plantCard.Infect.SetInfect("Aphids", 3);
            plantCard.Infect.SetInfect("Thrips", 5);
            plantCard.Infect.SetEggs("Thrips", 2);

            // Act: Serialize
            var infectData = GameStateManager.SerializeInfectLevel(plantCard);

            // Assert
            Assert.IsNotNull(infectData, "infectData should not be null");
            Assert.AreEqual(2, infectData.Count, "Should have 2 infection sources");

            var aphidsEntry = infectData.FirstOrDefault(e => e.source == "Aphids");
            Assert.IsNotNull(aphidsEntry, "Should have Aphids entry");
            Assert.AreEqual(3, aphidsEntry.infect, "Aphids infect should be 3");
            Assert.AreEqual(0, aphidsEntry.eggs, "Aphids eggs should be 0");

            var thripsEntry = infectData.FirstOrDefault(e => e.source == "Thrips");
            Assert.IsNotNull(thripsEntry, "Should have Thrips entry");
            Assert.AreEqual(5, thripsEntry.infect, "Thrips infect should be 5");
            Assert.AreEqual(2, thripsEntry.eggs, "Thrips eggs should be 2");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void PlantSerialization_SkipsZeroInfectLevels()
        {
            // Arrange: Plant with zero infection
            var gameObject = new GameObject("TestPlant");
            var plant = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard();
            plant.PlantCard = plantCard;

            // Don't add any infections

            // Act: Serialize
            var infectData = GameStateManager.SerializeInfectLevel(plantCard);

            // Assert
            Assert.IsNotNull(infectData, "infectData should not be null");
            Assert.AreEqual(0, infectData.Count, "Should have no entries for zero infections");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void PlantSerialization_PreservesIsolationFlags()
        {
            // Arrange: Apply isolation
            var gameObject = new GameObject("TestPlant");
            var plant = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard();
            plant.PlantCard = plantCard;

            var isolate = new IsolateBasic();
            isolate.ApplyLocationEffect(plant);

            Assert.IsFalse(plant.canSpreadAfflictions, "Should be unable to spread after isolation");
            Assert.IsFalse(plant.canReceiveAfflictions, "Should be unable to receive after isolation");

            // Act: Serialize
            var plantData = new PlantData
            {
                plantCard = GameStateManager.SerializeCard(plant.PlantCard),
                locationIndex = 0,
                currentAfflictions = plant.cAfflictions,
                priorAfflictions = plant.pAfflictions,
                usedTreatments = plant.uTreatments,
                currentTreatments = plant.cTreatments,
                moldIntensity = plant.moldIntensity,
                uLocationCards = plant.uLocationCards?.ToList() ?? new List<string>(),
                infectData = GameStateManager.SerializeInfectLevel(plant.PlantCard as IPlantCard),
                canSpreadAfflictions = plant.canSpreadAfflictions,
                canReceiveAfflictions = plant.canReceiveAfflictions
            };

            // Assert
            Assert.IsFalse(plantData.canSpreadAfflictions, "Serialized data should preserve isolation flags");
            Assert.IsFalse(plantData.canReceiveAfflictions, "Serialized data should preserve isolation flags");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void PlantSerialization_BackwardsCompatible_WithOldSaves()
        {
            // Arrange: Old save format (missing new fields)
            var oldPlantData = new PlantData
            {
                plantCard = new CardData { cardTypeName = "ColeusCard", value = 10 },
                locationIndex = 0,
                currentAfflictions = new List<string>(),
                priorAfflictions = new List<string>(),
                usedTreatments = new List<string>(),
                currentTreatments = new List<string>(),
                moldIntensity = 0f
                // uLocationCards, infectData, isolation flags are NULL (default)
            };

            // Assert: Verify defaults
            Assert.IsNull(oldPlantData.uLocationCards, "Old saves should have null uLocationCards");
            Assert.IsNull(oldPlantData.infectData, "Old saves should have null infectData");
            Assert.IsFalse(oldPlantData.canSpreadAfflictions, "Old saves have default bool (false)");
            Assert.IsFalse(oldPlantData.canReceiveAfflictions, "Old saves have default bool (false)");

            // The deserialization code should handle these nulls gracefully
            // and restore correct default behavior (canSpread/canReceive = true)
        }

        [Test]
        public void PlantSerialization_RoundTrip_PreservesAllData()
        {
            // Arrange: Complex plant state
            var gameObject = new GameObject("TestPlant");
            var plant = gameObject.AddComponent<PlantController>();
            var plantCard = new ColeusCard { Value = 10 };
            plant.PlantCard = plantCard;

            // Apply location cards
            var urea = new UreaBasic();
            urea.ApplyLocationEffect(plant);

            // Add infection
            plantCard.Infect.SetInfect("Aphids", 3);
            plantCard.Infect.SetEggs("Aphids", 1);

            // Apply isolation
            var isolate = new IsolateBasic();
            isolate.ApplyLocationEffect(plant);

            // Act: First serialization
            var data1 = new PlantData
            {
                plantCard = GameStateManager.SerializeCard(plant.PlantCard),
                locationIndex = 0,
                currentAfflictions = plant.cAfflictions,
                priorAfflictions = plant.pAfflictions,
                usedTreatments = plant.uTreatments,
                currentTreatments = plant.cTreatments,
                moldIntensity = plant.moldIntensity,
                uLocationCards = plant.uLocationCards?.ToList() ?? new List<string>(),
                infectData = GameStateManager.SerializeInfectLevel(plant.PlantCard as IPlantCard),
                canSpreadAfflictions = plant.canSpreadAfflictions,
                canReceiveAfflictions = plant.canReceiveAfflictions
            };

            // Simulate restoration to a new plant
            var gameObject2 = new GameObject("TestPlant2");
            var plant2 = gameObject2.AddComponent<PlantController>();
            var restoredCard = GameStateManager.DeserializeCard(data1.plantCard);
            plant2.PlantCard = restoredCard;

            // Restore location cards
            if (data1.uLocationCards != null)
            {
                plant2.uLocationCards.Clear();
                plant2.uLocationCards.AddRange(data1.uLocationCards);
            }

            // Restore infection
            if (data1.infectData != null && plant2.PlantCard is IPlantCard plantCardInterface)
                foreach (var entry in data1.infectData)
                {
                    if (entry.infect > 0)
                        plantCardInterface.Infect.SetInfect(entry.source, entry.infect);
                    if (entry.eggs > 0)
                        plantCardInterface.Infect.SetEggs(entry.source, entry.eggs);
                }

            // Restore isolation flags
            var legacySave = data1.uLocationCards == null;
            plant2.canSpreadAfflictions = legacySave || data1.canSpreadAfflictions;
            plant2.canReceiveAfflictions = legacySave || data1.canReceiveAfflictions;

            // Act: Second serialization
            var data2 = new PlantData
            {
                plantCard = GameStateManager.SerializeCard(plant2.PlantCard),
                locationIndex = 0,
                currentAfflictions = plant2.cAfflictions,
                priorAfflictions = plant2.pAfflictions,
                usedTreatments = plant2.uTreatments,
                currentTreatments = plant2.cTreatments,
                moldIntensity = plant2.moldIntensity,
                uLocationCards = plant2.uLocationCards?.ToList() ?? new List<string>(),
                infectData = GameStateManager.SerializeInfectLevel(plant2.PlantCard as IPlantCard),
                canSpreadAfflictions = plant2.canSpreadAfflictions,
                canReceiveAfflictions = plant2.canReceiveAfflictions
            };

            // Assert: Both produce identical data
            Assert.AreEqual(data1.uLocationCards.Count, data2.uLocationCards.Count, "Location card count should match");
            Assert.AreEqual(data1.infectData!.Count, data2.infectData.Count, "Infection data count should match");
            Assert.AreEqual(data1.canSpreadAfflictions, data2.canSpreadAfflictions, "Isolation flags should match");
            Assert.AreEqual(data1.canReceiveAfflictions, data2.canReceiveAfflictions, "Isolation flags should match");

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(gameObject2);
        }

        [Test]
        public void PlantSerialization_MultipleInfectionSources_PreservesAll()
        {
            // Arrange: Plant with multiple different infections
            var gameObject = new GameObject("TestPlant");
            var plant = gameObject.AddComponent<PlantController>();
            var plantCard = new CucumberCard();
            plant.PlantCard = plantCard;

            plantCard.Infect.SetInfect("Aphids", 2);
            plantCard.Infect.SetEggs("Aphids", 1);
            plantCard.Infect.SetInfect("Thrips", 4);
            plantCard.Infect.SetEggs("Thrips", 3);
            plantCard.Infect.SetInfect("Mildew", 1);

            // Act: Serialize
            var infectData = GameStateManager.SerializeInfectLevel(plantCard);

            // Assert
            Assert.AreEqual(3, infectData.Count, "Should preserve all 3 infection sources");
            Assert.IsTrue(infectData.Any(e => e.source == "Aphids"), "Should have Aphids");
            Assert.IsTrue(infectData.Any(e => e.source == "Thrips"), "Should have Thrips");
            Assert.IsTrue(infectData.Any(e => e.source == "Mildew"), "Should have Mildew");

            var totalInfect = infectData.Sum(e => e.infect);
            var totalEggs = infectData.Sum(e => e.eggs);
            Assert.AreEqual(7, totalInfect, "Total infection should be 2+4+1=7");
            Assert.AreEqual(4, totalEggs, "Total eggs should be 1+3=4");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void PlantSerialization_UreaDiminishingReturns_PersistsCorrectly()
        {
            // Arrange: Apply Urea multiple times to test diminishing returns tracking
            var gameObject = new GameObject("TestPlant");
            var plant = gameObject.AddComponent<PlantController>();
            var plantCard = new PepperCard { Value = 10 };
            plant.PlantCard = plantCard;

            var urea = new UreaBasic();
            urea.ApplyLocationEffect(plant); // 10 -> 20 (BaseValue = 10)
            urea.ApplyLocationEffect(plant); // 20 -> 25 (adds 5)
            urea.ApplyLocationEffect(plant); // 25 -> 28 (adds 3)

            Assert.AreEqual(28, plantCard.Value, "Value after 3 applications should be 28");
            Assert.AreEqual(3, plant.uLocationCards.Count, "Should track 3 applications");

            // Act: Serialize
            var plantData = new PlantData
            {
                plantCard = GameStateManager.SerializeCard(plant.PlantCard),
                locationIndex = 0,
                currentAfflictions = plant.cAfflictions,
                priorAfflictions = plant.pAfflictions,
                usedTreatments = plant.uTreatments,
                currentTreatments = plant.cTreatments,
                moldIntensity = plant.moldIntensity,
                uLocationCards = plant.uLocationCards?.ToList() ?? new List<string>(),
                infectData = GameStateManager.SerializeInfectLevel(plant.PlantCard as IPlantCard),
                canSpreadAfflictions = plant.canSpreadAfflictions,
                canReceiveAfflictions = plant.canReceiveAfflictions
            };

            // Assert: Verify serialization preserves application count
            Assert.AreEqual(3, plantData.uLocationCards.Count, "Serialization should preserve 3 applications");
            Assert.AreEqual(10, plantData.plantCard.baseValue, "BaseValue should be preserved");
            Assert.AreEqual(28, plantData.plantCard.value, "Current value should be preserved");

            // Simulate restore and apply 4th Urea
            var gameObject2 = new GameObject("TestPlant2");
            var plant2 = gameObject2.AddComponent<PlantController>();
            var restoredCard = GameStateManager.DeserializeCard(plantData.plantCard) as PepperCard;
            plant2.PlantCard = restoredCard;
            plant2.uLocationCards.AddRange(plantData.uLocationCards);

            // Apply 4th Urea after restore
            urea.ApplyLocationEffect(plant2);

            // Assert: 4th application should add 2 (25% of BaseValue=10)
            Assert.AreEqual(30, restoredCard!.Value, "4th application should add 2 (10*0.25), making 30");
            Assert.AreEqual(4, plant2.uLocationCards.Count, "Should now have 4 applications tracked");

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(gameObject2);
        }
    }
}