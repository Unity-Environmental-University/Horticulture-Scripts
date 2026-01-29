using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.Handlers;
using _project.Scripts.PlayModeTest.Utilities.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace _project.Scripts.PlayModeTest
{
    public class TreatmentEfficacyHandlerTests
    {
        private static int FindSeedMatchingRoll(Func<int, bool> predicate)
        {
            for (var seed = 0; seed < 10_000; seed++)
            {
                Random.InitState(seed);
                var roll = Random.Range(0, 100);
                if (!predicate(roll)) continue;
                return seed;
            }

            Assert.Fail("Failed to find a deterministic seed for the requested predicate.");
            return -1;
        }

        private class TestAffliction : PlantAfflictions.IAffliction
        {
            private readonly bool _canTreat;

            public TestAffliction(string name, bool canTreat)
            {
                Name = name;
                _canTreat = canTreat;
            }

            public string Name { get; }

            public string Description => string.Empty;
            public Color Color => Color.white;
            public Shader Shader => null;
            public List<PlantAfflictions.ITreatment> AcceptableTreatments { get; } = new();

            public bool CanBeTreatedBy(PlantAfflictions.ITreatment treatment)
            {
                return _canTreat;
            }

            public bool TreatWith(PlantAfflictions.ITreatment treatment, PlantController plant)
            {
                return _canTreat;
            }

            public void TickDay(PlantController plant)
            {
            }

            public PlantAfflictions.IAffliction Clone()
            {
                return new TestAffliction(Name, _canTreat);
            }
        }

        private class TestTreatment : PlantAfflictions.ITreatment
        {
            public TestTreatment(string name, int? efficacy)
            {
                Name = name;
                Efficacy = efficacy;
            }

            public string Name { get; }
            public string Description => string.Empty;
            public int? InfectCureValue { get; set; }
            public int? EggCureValue { get; set; }
            public int? Efficacy { get; set; }
        }

        #region Setup and Teardown

        private const int LargeDatasetSize = 1000; // Stress test with 1000 combinations

        [TearDown]
        public void TearDown()
        {
            // Clean up test file
            var testPath = GetTestFilePath();
            if (File.Exists(testPath)) File.Delete(testPath);

            // Clean up any lingering GameObjects from failed tests
            var handlers = Object.FindObjectsOfType<TreatmentEfficacyHandler>();
            foreach (var handler in handlers) Object.DestroyImmediate(handler.gameObject);
        }

        #endregion

        #region Helper Methods

        private static string GetTestFilePath()
        {
            return $"{Application.persistentDataPath}/discoveryData.json";
        }

        private static void CreateValidDiscoveryFile(params string[] keys)
        {
            var data = new DiscoveryData { discoveredComboHash = keys.ToList() };
            var json = JsonUtility.ToJson(data);
            File.WriteAllText(GetTestFilePath(), json);
        }

        private static void CreateCorruptedFile()
        {
            File.WriteAllText(GetTestFilePath(), "{ invalid json }}}");
        }

        private static void CreateEmptyFile()
        {
            File.WriteAllText(GetTestFilePath(), string.Empty);
        }

        #endregion

        #region Existing Tests

        [Test]
        public void GetRelationalEfficacy_ReturnsTreatmentEfficacyForNewCombination()
        {
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            var affliction = new TestAffliction("Leaf Spot", true);
            var treatment = new TestTreatment("Standard Spray", 75);

            var result = handler.GetRelationalEfficacy(affliction, treatment);

            Assert.AreEqual(75, result);

            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void GetRelationalEfficacy_ReturnsZeroWhenTreatmentCannotBeApplied()
        {
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            var affliction = new TestAffliction("Leaf Spot", false);
            var treatment = new TestTreatment("Standard Spray", 90);

            var result = handler.GetRelationalEfficacy(affliction, treatment);

            Assert.AreEqual(0, result);

            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void GetRelationalEfficacy_ReusesExistingEntryByName()
        {
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            var affliction = new TestAffliction("Leaf Spot", true);
            var treatment = new TestTreatment("Standard Spray", 60);
            var initial = handler.GetRelationalEfficacy(affliction, treatment);

            var sameNameAffliction = new TestAffliction("Leaf Spot", true);
            var sameNameTreatment = new TestTreatment("Standard Spray", 100);
            var repeat = handler.GetRelationalEfficacy(sameNameAffliction, sameNameTreatment);

            Assert.AreEqual(initial, repeat);

            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void GetRelationalEfficacy_PreviewDoesNotMutateState()
        {
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            var affliction = new TestAffliction("Leaf Spot", true);
            var treatment = new TestTreatment("Standard Spray", 75);

            var storageField = typeof(TreatmentEfficacyHandler)
                .GetField("relationalEfficacies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(storageField, "Expected to access relational efficacy cache via reflection for testing.");

            var cache = (List<RelationalEfficacy>)storageField.GetValue(handler);
            Assert.IsNotNull(cache);

            var preview = handler.GetRelationalEfficacy(affliction, treatment, false);
            Assert.AreEqual(75, preview);
            Assert.AreEqual(0, cache!.Count);

            var actual = handler.GetRelationalEfficacy(affliction, treatment);
            Assert.AreEqual(75, actual);
            Assert.AreEqual(1, cache.Count);
            Assert.AreEqual(1, cache[0].interactionCount);

            handler.GetRelationalEfficacy(affliction, treatment, false);
            Assert.AreEqual(1, cache[0].interactionCount);

            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void TouchEfficacy_ReducesValueWhenRollIsBelowThreshold()
        {
            var relational = new RelationalEfficacy
            {
                efficacy = 50,
                interactionCount = 6
            };

            var seed = FindSeedMatchingRoll(value => value < 80);
            Random.InitState(seed);

            relational.TouchEfficacy();

            Assert.AreEqual(40, relational.efficacy);
        }

        [Test]
        public void TouchEfficacy_DoesNotReduceWhenRollIsAboveThreshold()
        {
            var relational = new RelationalEfficacy
            {
                efficacy = 50,
                interactionCount = 12
            };

            var seed = FindSeedMatchingRoll(value => value >= 50);
            Random.InitState(seed);

            relational.TouchEfficacy();

            Assert.AreEqual(50, relational.efficacy);
        }

        [Test]
        public void TouchEfficacy_ClampsToMinimumOfOne()
        {
            var relational = new RelationalEfficacy
            {
                efficacy = 5,
                interactionCount = 20
            };

            var seed = FindSeedMatchingRoll(value => value < 30);
            Random.InitState(seed);

            relational.TouchEfficacy();

            Assert.AreEqual(1, relational.efficacy);
        }

        [Test]
        public void GetAverageEfficacy_FiltersIncompatibleAfflictions()
        {
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            var plantGo = new GameObject("TestPlant");
            var plantController = plantGo.AddComponent<PlantController>();

            // Create one treatable and one incompatible affliction
            var treatableAffliction = new TestAffliction("Treatable", true);
            var incompatibleAffliction = new TestAffliction("Incompatible", false);
            var treatment = new TestTreatment("Test Treatment", 80);

            // Add afflictions directly to the list (CurrentAfflictions is a get-only property)
            plantController.CurrentAfflictions.Add(treatableAffliction);
            plantController.CurrentAfflictions.Add(incompatibleAffliction);

            var average = handler.GetAverageEfficacy(treatment, plantController);

            // Should return efficacy for treatable only (80), ignoring incompatible (0)
            Assert.AreEqual(80, average);

            Object.DestroyImmediate(plantGo);
            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void GetAverageEfficacy_ReturnsZeroWhenNoTreatableAfflictions()
        {
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            var plantGo = new GameObject("TestPlant");
            var plantController = plantGo.AddComponent<PlantController>();

            // Create only incompatible afflictions
            var incompatible1 = new TestAffliction("Incompatible1", false);
            var incompatible2 = new TestAffliction("Incompatible2", false);
            var treatment = new TestTreatment("Test Treatment", 90);

            // Add afflictions directly to the list
            plantController.CurrentAfflictions.Add(incompatible1);
            plantController.CurrentAfflictions.Add(incompatible2);

            var average = handler.GetAverageEfficacy(treatment, plantController);

            Assert.AreEqual(0, average);

            Object.DestroyImmediate(plantGo);
            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void GetAverageEfficacy_AveragesMultipleTreatableAfflictions()
        {
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            var plantGo = new GameObject("TestPlant");
            var plantController = plantGo.AddComponent<PlantController>();

            // Create two treatable afflictions
            var affliction1 = new TestAffliction("Affliction1", true);
            var affliction2 = new TestAffliction("Affliction2", true);
            var treatment = new TestTreatment("Treatment", 100);

            // Manually seed the handler with different efficacy values via reflection
            var storageField = typeof(TreatmentEfficacyHandler)
                .GetField("relationalEfficacies", BindingFlags.Instance | BindingFlags.NonPublic);
            var cache = (List<RelationalEfficacy>)storageField?.GetValue(handler);

            // Create two RelationalEfficacy entries with different efficacies (80 and 60)
            var rel1 = new RelationalEfficacy
            {
                affliction = affliction1,
                treatment = treatment,
                efficacy = 80,
                interactionCount = 1
            };
            rel1.SetNames(affliction1, treatment);

            var rel2 = new RelationalEfficacy
            {
                affliction = affliction2,
                treatment = treatment,
                efficacy = 60,
                interactionCount = 1
            };
            rel2.SetNames(affliction2, treatment);

            cache!.Add(rel1);
            cache.Add(rel2);

            // Add afflictions directly to the list
            plantController.CurrentAfflictions.Add(affliction1);
            plantController.CurrentAfflictions.Add(affliction2);

            var average = handler.GetAverageEfficacy(treatment, plantController);

            // Should average 80 and 60 = 70
            Assert.AreEqual(70, average);

            Object.DestroyImmediate(plantGo);
            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void GetAverageEfficacy_DoesNotCountInteraction()
        {
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            var plantGo = new GameObject("TestPlant");
            var plantController = plantGo.AddComponent<PlantController>();

            var affliction = new TestAffliction("Test", true);
            var treatment = new TestTreatment("Treatment", 75);

            // Add affliction directly to the list
            plantController.CurrentAfflictions.Add(affliction);

            var storageField = typeof(TreatmentEfficacyHandler)
                .GetField("relationalEfficacies", BindingFlags.Instance | BindingFlags.NonPublic);
            var cache = (List<RelationalEfficacy>)storageField?.GetValue(handler);

            // Call GetAverageEfficacy multiple times
            handler.GetAverageEfficacy(treatment, plantController);
            handler.GetAverageEfficacy(treatment, plantController);

            // Should not add any entries to cache (preview mode)
            Assert.AreEqual(0, cache!.Count);

            Object.DestroyImmediate(plantGo);
            Object.DestroyImmediate(handlerGo);
        }

        #endregion

        #region Basic Save/Load Tests

        [Test]
        public void SaveDiscoveryState_CreatesFileWithValidJSON()
        {
            // Arrange
            var combinations = new HashSet<string> { "Treatment1|Affliction1", "Treatment2|Affliction2" };

            // Act
            TreatmentEfficacyHandlerReflection.InvokeSaveDiscoveryState(combinations);

            // Assert
            Assert.IsTrue(File.Exists(GetTestFilePath()), "Discovery file should be created");

            var fileContent = File.ReadAllText(GetTestFilePath());
            Assert.IsFalse(string.IsNullOrEmpty(fileContent), "File should contain data");

            var loadedData = JsonUtility.FromJson<DiscoveryData>(fileContent);
            Assert.IsNotNull(loadedData, "JSON should be valid");
            Assert.AreEqual(2, loadedData.discoveredComboHash.Count, "Should contain 2 combinations");
        }

        [Test]
        public void LoadDiscoveryData_RestoresDiscoveredCombinations()
        {
            // Arrange
            CreateValidDiscoveryFile("Treatment1|Affliction1", "Treatment2|Affliction2");

            // Act
            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();

            // Assert
            Assert.IsNotNull(loadedData, "Loaded data should not be null");
            Assert.AreEqual(2, loadedData.discoveredComboHash.Count, "Should load 2 combinations");
            Assert.IsTrue(loadedData.discoveredComboHash.Contains("Treatment1|Affliction1"));
            Assert.IsTrue(loadedData.discoveredComboHash.Contains("Treatment2|Affliction2"));
        }

        [Test]
        public void SaveAndLoad_RoundTrip_PreservesData()
        {
            // Arrange
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            var originalCombinations = new HashSet<string>
            {
                "Treatment1|Affliction1",
                "Treatment2|Affliction2",
                "Treatment3|Affliction3"
            };

            // Act - Save
            TreatmentEfficacyHandlerReflection.SetDiscoveredCombinations(handler, originalCombinations);
            TreatmentEfficacyHandlerReflection.InvokeSaveDiscoveryState(originalCombinations);

            // Destroy handler and create new one to simulate reload
            Object.DestroyImmediate(handlerGo);

            // Act - Load
            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();
            var loadedCombinations = new HashSet<string>(loadedData.discoveredComboHash);

            // Assert
            Assert.AreEqual(originalCombinations.Count, loadedCombinations.Count, "Count should match");
            foreach (var combo in originalCombinations)
                Assert.IsTrue(loadedCombinations.Contains(combo), $"Should contain {combo}");
        }

        [Test]
        public void IsDiscovered_ReflectsSavedStateAfterReload()
        {
            // Arrange - Create and save discoveries
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();
            handler.DiscoveryModeEnabled = true;

            var combinations = new HashSet<string> { "TreatmentA|AfflictionA" };
            TreatmentEfficacyHandlerReflection.SetDiscoveredCombinations(handler, combinations);
            TreatmentEfficacyHandlerReflection.InvokeSaveDiscoveryState(combinations);

            Object.DestroyImmediate(handlerGo);

            // Act - Create new handler (simulates reload)
            var newHandlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var newHandler = newHandlerGo.AddComponent<TreatmentEfficacyHandler>();
            newHandler.DiscoveryModeEnabled = true;

            // Load data and populate the handler
            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();
            var loadedCombinations = new HashSet<string>(loadedData.discoveredComboHash);
            TreatmentEfficacyHandlerReflection.SetDiscoveredCombinations(newHandler, loadedCombinations);

            // Assert
            Assert.IsTrue(newHandler.IsDiscovered("TreatmentA", "AfflictionA"),
                "Previously discovered combination should be marked as discovered");
            Assert.IsFalse(newHandler.IsDiscovered("TreatmentB", "AfflictionB"),
                "New combination should not be discovered");

            Object.DestroyImmediate(newHandlerGo);
        }

        [Test]
        public void SaveDiscoveryState_EmptyDiscoveries_CreatesValidFile()
        {
            // Arrange
            var emptyCombinations = new HashSet<string>();

            // Act
            TreatmentEfficacyHandlerReflection.InvokeSaveDiscoveryState(emptyCombinations);

            // Assert
            Assert.IsTrue(File.Exists(GetTestFilePath()), "File should be created even for empty data");

            var fileContent = File.ReadAllText(GetTestFilePath());
            var loadedData = JsonUtility.FromJson<DiscoveryData>(fileContent);

            Assert.IsNotNull(loadedData, "Should deserialize without error");
            Assert.AreEqual(0, loadedData.discoveredComboHash.Count, "Should contain no combinations");
        }

        [Test]
        public void LoadDiscoveryData_FileDoesNotExist_ReturnsEmptyData()
        {
            // Arrange - Ensure no file exists
            if (File.Exists(GetTestFilePath())) File.Delete(GetTestFilePath());

            // Act
            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();

            // Assert
            Assert.IsNotNull(loadedData, "Should return non-null data");
            Assert.AreEqual(0, loadedData.discoveredComboHash.Count, "Should return empty list");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void LoadDiscoveryData_CorruptedJSON_HandlesGracefully()
        {
            // Arrange
            CreateCorruptedFile();

            // Act
            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();

            // Assert - Should return empty data, not crash
            Assert.IsNotNull(loadedData, "Should return non-null data even with corrupted file");
            Assert.AreEqual(0, loadedData.discoveredComboHash.Count, "Should return empty list for corrupted data");
        }

        [Test]
        public void LoadDiscoveryData_EmptyFile_ReturnsEmptyData()
        {
            // Arrange
            CreateEmptyFile();

            // Act
            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();

            // Assert
            Assert.IsNotNull(loadedData, "Should return non-null data");
            Assert.AreEqual(0, loadedData.discoveredComboHash.Count, "Should return empty list for empty file");
        }

        [Test]
        public void SaveDiscoveryState_LargeDataset_SuccessfullySerializes()
        {
            // Arrange - Create large dataset
            var largeCombinations = new HashSet<string>();
            for (var i = 0; i < LargeDatasetSize; i++) largeCombinations.Add($"Treatment{i}|Affliction{i}");

            // Act
            TreatmentEfficacyHandlerReflection.InvokeSaveDiscoveryState(largeCombinations);

            // Assert
            Assert.IsTrue(File.Exists(GetTestFilePath()), "File should be created");

            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();
            Assert.AreEqual(LargeDatasetSize, loadedData.discoveredComboHash.Count,
                $"All {LargeDatasetSize} combinations should be preserved");

            // Verify a few samples
            Assert.IsTrue(loadedData.discoveredComboHash.Contains("Treatment0|Affliction0"));
            Assert.IsTrue(loadedData.discoveredComboHash.Contains("Treatment500|Affliction500"));
            Assert.IsTrue(
                loadedData.discoveredComboHash.Contains(
                    $"Treatment{LargeDatasetSize - 1}|Affliction{LargeDatasetSize - 1}"));
        }

        [Test]
        public void SaveDiscoveryState_SpecialCharactersInNames_SerializesCorrectly()
        {
            // Arrange - Test various special characters
            var specialCombinations = new HashSet<string>
            {
                "Treatment \"Quotes\"|Affliction 'Apostrophes'",
                "Treatment|With|Pipes|Affliction|Too",
                "Treatment\nNewline|Affliction\tTab",
                "Treatment $pecial!|Affliction #Char$"
            };

            // Act
            TreatmentEfficacyHandlerReflection.InvokeSaveDiscoveryState(specialCombinations);

            // Assert
            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();
            var loadedCombinations = new HashSet<string>(loadedData.discoveredComboHash);

            Assert.AreEqual(specialCombinations.Count, loadedCombinations.Count, "Count should match");
            foreach (var combo in specialCombinations)
                Assert.IsTrue(loadedCombinations.Contains(combo), $"Should preserve special characters in: {combo}");
        }

        [Test]
        public void MakeDiscoveryKey_ConsistentFormat()
        {
            // Arrange
            var treatmentName = "Test Treatment";
            var afflictionName = "Test Affliction";

            // Act
            var key = TreatmentEfficacyHandlerReflection.InvokeMakeDiscoveryKey(treatmentName, afflictionName);

            // Assert
            Assert.AreEqual("Test Treatment|Test Affliction", key,
                "Key should follow '{treatment}|{affliction}' format");

            // Verify consistency
            var key2 = TreatmentEfficacyHandlerReflection.InvokeMakeDiscoveryKey(treatmentName, afflictionName);
            Assert.AreEqual(key, key2, "Same inputs should produce identical keys");
        }

        [Test]
        public void LoadDiscoveryData_DuplicatesInFile_DeduplicatesInMemory()
        {
            // Arrange - Manually create file with duplicates
            var duplicateData = new DiscoveryData
            {
                discoveredComboHash = new List<string>
                {
                    "Treatment1|Affliction1",
                    "Treatment2|Affliction2",
                    "Treatment1|Affliction1", // Duplicate
                    "Treatment3|Affliction3",
                    "Treatment2|Affliction2" // Duplicate
                }
            };
            var json = JsonUtility.ToJson(duplicateData);
            File.WriteAllText(GetTestFilePath(), json);

            // Act
            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();
            var uniqueCombinations = new HashSet<string>(loadedData.discoveredComboHash);

            // Assert
            Assert.AreEqual(5, loadedData.discoveredComboHash.Count,
                "List should contain all entries including duplicates");
            Assert.AreEqual(3, uniqueCombinations.Count, "HashSet should deduplicate to 3 unique entries");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void MarkAsDiscovered_NewCombination_TriggersSave()
        {
            // Arrange
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();
            handler.DiscoveryModeEnabled = true;

            // Ensure file doesn't exist before test
            if (File.Exists(GetTestFilePath())) File.Delete(GetTestFilePath());

            // Act
            TreatmentEfficacyHandlerReflection.InvokeMarkAsDiscovered(handler, "Treatment1", "Affliction1", 75);

            // Assert
            Assert.IsTrue(File.Exists(GetTestFilePath()), "File should be created after marking as discovered");

            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();
            Assert.AreEqual(1, loadedData.discoveredComboHash.Count, "Should contain 1 combination");
            Assert.IsTrue(loadedData.discoveredComboHash.Contains("Treatment1|Affliction1"));

            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void MarkAsDiscovered_ExistingCombination_DoesNotTriggerSave()
        {
            // Arrange
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();
            handler.DiscoveryModeEnabled = true;

            // Pre-populate with one discovery
            var combinations = new HashSet<string> { "Treatment1|Affliction1" };
            TreatmentEfficacyHandlerReflection.SetDiscoveredCombinations(handler, combinations);
            TreatmentEfficacyHandlerReflection.InvokeSaveDiscoveryState(combinations);

            // Read file content before re-marking
            var originalFileContent = File.ReadAllText(GetTestFilePath());
            var originalFileLength = new FileInfo(GetTestFilePath()).Length;

            // Act - Mark the same combination again
            TreatmentEfficacyHandlerReflection.InvokeMarkAsDiscovered(handler, "Treatment1", "Affliction1", 75);

            // Assert - File should not be modified
            var newFileContent = File.ReadAllText(GetTestFilePath());
            var newFileLength = new FileInfo(GetTestFilePath()).Length;

            Assert.AreEqual(originalFileContent, newFileContent,
                "File content should not change when marking existing combination");
            Assert.AreEqual(originalFileLength, newFileLength,
                "File size should not change when marking existing combination");

            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void IsDiscovered_DiscoveryModeDisabled_AlwaysReturnsTrue()
        {
            // Arrange
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();
            handler.DiscoveryModeEnabled = false;

            // Empty discoveries - nothing discovered
            var emptyCombinations = new HashSet<string>();
            TreatmentEfficacyHandlerReflection.SetDiscoveredCombinations(handler, emptyCombinations);

            // Act & Assert
            Assert.IsTrue(handler.IsDiscovered("AnyTreatment", "AnyAffliction"),
                "Should return true when discovery mode disabled");
            Assert.IsTrue(handler.IsDiscovered("Another", "Combo"), "Should return true for any combination");

            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void IsDiscovered_DiscoveryModeEnabled_RespectsActualState()
        {
            // Arrange
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();
            handler.DiscoveryModeEnabled = true;

            var combinations = new HashSet<string> { "Known|Combo" };
            TreatmentEfficacyHandlerReflection.SetDiscoveredCombinations(handler, combinations);

            // Act & Assert
            Assert.IsTrue(handler.IsDiscovered("Known", "Combo"), "Should return true for discovered combination");
            Assert.IsFalse(handler.IsDiscovered("Unknown", "Combo"),
                "Should return false for undiscovered combination");

            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void ClearDiscoveredCombinations_DeletesFile()
        {
            // Arrange
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            var combinations = new HashSet<string> { "Treatment1|Affliction1", "Treatment2|Affliction2" };
            TreatmentEfficacyHandlerReflection.SetDiscoveredCombinations(handler, combinations);
            TreatmentEfficacyHandlerReflection.InvokeSaveDiscoveryState(combinations);

            Assert.IsTrue(File.Exists(GetTestFilePath()), "File should exist before clearing");

            // Act
            handler.ClearDiscoveredCombinations();

            // Assert
            Assert.IsFalse(File.Exists(GetTestFilePath()), "File should be deleted after clearing");

            var inMemoryCombinations = TreatmentEfficacyHandlerReflection.GetDiscoveredCombinations(handler);
            Assert.AreEqual(0, inMemoryCombinations.Count, "In-memory HashSet should be empty");

            Object.DestroyImmediate(handlerGo);
        }

        [Test]
        public void GetRelationalEfficacy_DiscoveryIntegration_SavesOnFirstUse()
        {
            // Arrange
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();
            handler.DiscoveryModeEnabled = true;

            var affliction = new TestAffliction("TestAffliction", true);
            var treatment = new TestTreatment("TestTreatment", 80);

            // Ensure file doesn't exist before test
            if (File.Exists(GetTestFilePath())) File.Delete(GetTestFilePath());

            // Act - Call with countInteraction=true (default)
            var efficacy = handler.GetRelationalEfficacy(affliction, treatment);

            // Assert
            Assert.AreEqual(80, efficacy, "Should return correct efficacy");
            Assert.IsTrue(File.Exists(GetTestFilePath()), "File should be created after first interaction");

            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();
            Assert.AreEqual(1, loadedData.discoveredComboHash.Count, "Should save the discovery");
            Assert.IsTrue(loadedData.discoveredComboHash.Contains("TestTreatment|TestAffliction"));

            Object.DestroyImmediate(handlerGo);
        }

        #endregion

        #region Data Integrity Tests

        [Test]
        public void SaveDiscoveryState_MultipleSaveCycles_NoDataCorruption()
        {
            // Arrange
            var combinations = new HashSet<string>();

            // Act - 5 save cycles, adding data each time
            for (var cycle = 1; cycle <= 5; cycle++)
            {
                combinations.Add($"Treatment{cycle}|Affliction{cycle}");
                TreatmentEfficacyHandlerReflection.InvokeSaveDiscoveryState(combinations);

                // Verify after each save
                var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();
                Assert.AreEqual(cycle, loadedData.discoveredComboHash.Count,
                    $"Should have {cycle} combinations after cycle {cycle}");
            }

            // Assert - Final verification
            var finalData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();
            var finalCombinations = new HashSet<string>(finalData.discoveredComboHash);

            Assert.AreEqual(5, finalCombinations.Count, "Should have all 5 combinations");
            for (var i = 1; i <= 5; i++)
                Assert.IsTrue(finalCombinations.Contains($"Treatment{i}|Affliction{i}"),
                    $"Should contain Treatment{i}|Affliction{i}");
        }

        [Test]
        public void HashSetToListToHashSet_PreservesUniqueness()
        {
            // Arrange
            var originalHashSet = new HashSet<string>
            {
                "Treatment1|Affliction1",
                "Treatment2|Affliction2",
                "Treatment3|Affliction3"
            };
            var originalCount = originalHashSet.Count;

            // Act - Convert HashSet -> List -> Save -> Load -> List -> HashSet
            TreatmentEfficacyHandlerReflection.InvokeSaveDiscoveryState(originalHashSet);
            var loadedData = TreatmentEfficacyHandlerReflection.InvokeLoadDiscoveryData();
            var restoredHashSet = new HashSet<string>(loadedData.discoveredComboHash);

            // Assert
            Assert.AreEqual(originalCount, restoredHashSet.Count, "Count should remain the same through conversions");
            foreach (var combo in originalHashSet)
                Assert.IsTrue(restoredHashSet.Contains(combo), $"Should preserve {combo}");
        }

        [Test]
        public void Awake_LoadsExistingData_BeforeFirstInteraction()
        {
            // Arrange - Pre-create discovery file
            CreateValidDiscoveryFile("PreExisting|Combination");

            // Act - Create handler (Awake automatically loads the file)
            var handlerGo = new GameObject(nameof(TreatmentEfficacyHandler));
            var handler = handlerGo.AddComponent<TreatmentEfficacyHandler>();

            // Assert - Handler should have the pre-existing combination loaded by Awake()
            var inMemoryCombinations = TreatmentEfficacyHandlerReflection.GetDiscoveredCombinations(handler);
            Assert.AreEqual(1, inMemoryCombinations.Count, "Should load 1 combination from file during Awake()");
            Assert.IsTrue(inMemoryCombinations.Contains("PreExisting|Combination"),
                "Should contain the pre-existing combination");

            // Verify IsDiscovered behavior (always returns true in both modes)
            // - When discovery mode enabled: loaded combination should be marked discovered → true
            // - When discovery mode disabled: IsDiscovered always returns true regardless
            Assert.IsTrue(handler.IsDiscovered("PreExisting", "Combination"),
                "IsDiscovered should return true for loaded combination (in both discovery modes)");

            Object.DestroyImmediate(handlerGo);
        }

        #endregion
    }
}