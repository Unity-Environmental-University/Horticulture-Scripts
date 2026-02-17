using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.GameState;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace _project.Scripts.PlayModeTest
{
    public class EnvironmentUpgradeTests
    {
        private class TestUpgradeA : IEnvironmentUpgrade
        {
            public TestUpgradeA(GameObject prefab)
            {
                Prefab = prefab;
            }

            public ICard Card => null;
            public Material DisplayMaterial => null;
            public GameObject Prefab { get; }
            public Material IconMaterial => null;
            public UpgradeDuration Duration => UpgradeDuration.OneRound;
            public string DisplayName => "Test Upgrade A";
            public int Cost => 0;

            public IBonus CalculateRoundBonus(int healthyPlantCount, int totalPlantCount)
            {
                return null;
            }

            public void Purchase() { }
        }

        private class TestUpgradeB : IEnvironmentUpgrade
        {
            public TestUpgradeB(GameObject prefab)
            {
                Prefab = prefab;
            }

            public ICard Card => null;
            public Material DisplayMaterial => null;
            public GameObject Prefab { get; }
            public Material IconMaterial => null;
            public UpgradeDuration Duration => UpgradeDuration.OneRound;
            public string DisplayName => "Test Upgrade B";
            public int Cost => 0;

            public IBonus CalculateRoundBonus(int healthyPlantCount, int totalPlantCount)
            {
                return null;
            }

            public void Purchase() { }
        }

        private GameObject _testGameObject;
        private EnvironmentUpgradeManager _manager;
        private ScoreManager _scoreManager;

        private static void SetUpgradeSpawnPoints(EnvironmentUpgradeManager manager, List<Transform> spawnPoints)
        {
            var field = typeof(EnvironmentUpgradeManager)
                .GetField("upgradeSpawnPoints", BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(manager, spawnPoints);
        }

        [SetUp]
        public void Setup()
        {
            _testGameObject = new GameObject("TestUpgradeManager");
            _manager = _testGameObject.AddComponent<EnvironmentUpgradeManager>();
            _scoreManager = _testGameObject.AddComponent<ScoreManager>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_testGameObject != null)
                Object.DestroyImmediate(_testGameObject);
        }

        [Test]
        public void BeeBox_CalculatesCorrectBonus()
        {
            // Arrange
            var beeBox = new BeeBox();

            // Act - 3 healthy plants
            var bonus = beeBox.CalculateRoundBonus(3, 5);

            // Assert
            Assert.IsNotNull(bonus, "Bonus should not be null");
            Assert.AreEqual("Bee Box", bonus.Name);
            Assert.AreEqual(6, bonus.BonusValue, "3 healthy plants * $2 = $6");
        }

        [Test]
        public void BeeBox_ReturnsNullBonus_WhenNoHealthyPlants()
        {
            // Arrange
            var beeBox = new BeeBox();

            // Act
            var bonus = beeBox.CalculateRoundBonus(0, 5);

            // Assert
            Assert.IsNull(bonus, "Bonus should be null when no healthy plants");
        }

        [Test]
        public void BeeBox_HasCorrectProperties()
        {
            // Arrange
            var beeBox = new BeeBox();

            // Assert
            Assert.AreEqual("Bee Box", beeBox.DisplayName);
            Assert.AreEqual(15, beeBox.Cost);
            Assert.AreEqual(2, BeeBox.BonusPerPlant);
            Assert.AreEqual(UpgradeDuration.OneRound, beeBox.Duration);
            Assert.IsNull(beeBox.Card, "Environment upgrades are not cards");
        }

        [Test]
        public void PurchaseUpgrade_AddsToActiveList()
        {
            // Arrange
            var beeBox = new BeeBox();

            // Act
            _manager.PurchaseUpgrade(beeBox);

            // Assert
            Assert.AreEqual(1, _manager.ActiveUpgrades.Count);
            Assert.AreSame(beeBox, _manager.ActiveUpgrades[0]);
        }

        [Test]
        public void PurchaseUpgrade_PreventsDuplicates()
        {
            // Arrange
            var beeBox1 = new BeeBox();
            var beeBox2 = new BeeBox();

            // Act
            _manager.PurchaseUpgrade(beeBox1);
            _manager.PurchaseUpgrade(beeBox2); // Should be rejected

            // Assert
            Assert.AreEqual(1, _manager.ActiveUpgrades.Count, "Should not add duplicate upgrade type");
        }

        [Test]
        public void PurchaseUpgrade_RejectsWhenSpawnSlotsAreFull()
        {
            // Arrange
            var spawnPoint = new GameObject("UpgradeSpawnPoint");
            SetUpgradeSpawnPoints(_manager, new List<Transform> { spawnPoint.transform });

            var prefabA = new GameObject("UpgradePrefabA");
            var prefabB = new GameObject("UpgradePrefabB");
            var upgradeA = new TestUpgradeA(prefabA);
            var upgradeB = new TestUpgradeB(prefabB);

            // Act
            var firstPurchase = _manager.PurchaseUpgrade(upgradeA);
            var secondPurchase = _manager.PurchaseUpgrade(upgradeB);

            // Assert
            Assert.IsTrue(firstPurchase, "First purchase should succeed when a slot is available");
            Assert.IsFalse(secondPurchase, "Second purchase should fail when all slots are occupied");
            Assert.AreEqual(1, _manager.ActiveUpgrades.Count, "Only one upgrade should be active");

            Object.DestroyImmediate(prefabA);
            Object.DestroyImmediate(prefabB);
            Object.DestroyImmediate(spawnPoint);
        }

        [Test]
        public void InjectBonuses_AddsBonusesToScoreManager()
        {
            // Arrange
            var beeBox = new BeeBox();
            _manager.PurchaseUpgrade(beeBox);

            // Act
            _manager.InjectBonuses(_scoreManager, 4, 5);

            // Assert
            Assert.AreEqual(1, _scoreManager.bonuses.Count);
            Assert.AreEqual("Bee Box", _scoreManager.bonuses[0].Name);
            Assert.AreEqual(8, _scoreManager.bonuses[0].BonusValue); // 4 healthy * $2
        }

        [Test]
        public void InjectBonuses_SkipsNullBonuses()
        {
            // Arrange
            var beeBox = new BeeBox();
            _manager.PurchaseUpgrade(beeBox);

            // Act - No healthy plants
            _manager.InjectBonuses(_scoreManager, 0, 5);

            // Assert
            Assert.AreEqual(0, _scoreManager.bonuses.Count, "No bonus should be added for 0 healthy plants");
        }

        [Test]
        public void ClearRoundUpgrades_RemovesOnlyOneRoundUpgrades()
        {
            // Arrange
            var beeBox = new BeeBox(); // OneRound duration
            _manager.PurchaseUpgrade(beeBox);

            // Act
            _manager.ClearRoundUpgrades();

            // Assert
            Assert.AreEqual(0, _manager.ActiveUpgrades.Count, "OneRound upgrades should be cleared");
        }

        [Test]
        public void ClearUpgrades_RemovesAllUpgrades()
        {
            // Arrange
            var beeBox = new BeeBox();
            _manager.PurchaseUpgrade(beeBox);

            // Act
            _manager.ClearUpgrades();

            // Assert
            Assert.AreEqual(0, _manager.ActiveUpgrades.Count);
        }

        [Test]
        public void SerializeUpgrades_ReturnsTypeNames()
        {
            // Arrange
            var beeBox = new BeeBox();
            _manager.PurchaseUpgrade(beeBox);

            // Act
            var serialized = _manager.SerializeUpgrades();

            // Assert
            Assert.AreEqual(1, serialized.Count);
            Assert.AreEqual(typeof(BeeBox).FullName, serialized[0]);
        }

        [Test]
        public void RestoreUpgrades_RecreatesUpgrades()
        {
            // Arrange
            var typeNames = new List<string> { typeof(BeeBox).FullName };

            // Act
            _manager.RestoreUpgrades(typeNames);

            // Assert
            Assert.AreEqual(1, _manager.ActiveUpgrades.Count);
            Assert.IsInstanceOf<BeeBox>(_manager.ActiveUpgrades[0]);
        }

        [Test]
        public void RestoreUpgrades_HandlesEmptyList()
        {
            // Act
            _manager.RestoreUpgrades(new List<string>());

            // Assert
            Assert.AreEqual(0, _manager.ActiveUpgrades.Count);
        }

        [Test]
        public void RestoreUpgrades_HandlesInvalidTypeName()
        {
            // Arrange
            var typeNames = new List<string> { "Invalid.Type.Name" };

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _manager.RestoreUpgrades(typeNames));
            Assert.AreEqual(0, _manager.ActiveUpgrades.Count);
        }

        [Test]
        public void EnvironmentUpgradeData_SerializesCorrectly()
        {
            // Arrange
            var data = new EnvironmentUpgradeData
            {
                activeUpgradeTypeNames = new List<string> { typeof(BeeBox).FullName }
            };

            // Act
            var json = JsonUtility.ToJson(data);
            var deserialized = JsonUtility.FromJson<EnvironmentUpgradeData>(json);

            // Assert
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.activeUpgradeTypeNames.Count);
            Assert.AreEqual(typeof(BeeBox).FullName, deserialized.activeUpgradeTypeNames[0]);
        }

        [UnityTest]
        public IEnumerator FullWorkflow_PurchaseAndCalculateBonus()
        {
            // Arrange
            var beeBox = new BeeBox();

            // Act - Purchase
            _manager.PurchaseUpgrade(beeBox);
            yield return null;

            // Act - Calculate bonus for 5 healthy plants
            _manager.InjectBonuses(_scoreManager, 5, 6);

            // Assert
            Assert.AreEqual(1, _scoreManager.bonuses.Count);
            Assert.AreEqual(10, _scoreManager.bonuses[0].BonusValue); // 5 * $2
        }

        [UnityTest]
        public IEnumerator FullWorkflow_PersistenceCycle()
        {
            // Arrange
            var beeBox = new BeeBox();
            _manager.PurchaseUpgrade(beeBox);
            yield return null;

            // Act - Serialize
            var serialized = _manager.SerializeUpgrades();

            // Act - Clear and restore
            _manager.ClearUpgrades();
            Assert.AreEqual(0, _manager.ActiveUpgrades.Count, "Should be cleared");
            yield return null;

            _manager.RestoreUpgrades(serialized);

            // Assert
            Assert.AreEqual(1, _manager.ActiveUpgrades.Count);
            Assert.IsInstanceOf<BeeBox>(_manager.ActiveUpgrades[0]);

            // Verify functionality after restore
            _manager.InjectBonuses(_scoreManager, 3, 5);
            Assert.AreEqual(1, _scoreManager.bonuses.Count);
            Assert.AreEqual(6, _scoreManager.bonuses[0].BonusValue);
        }
    }
}
