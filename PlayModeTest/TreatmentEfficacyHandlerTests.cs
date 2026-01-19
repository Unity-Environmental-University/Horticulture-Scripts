using System;
using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.Handlers;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace _project.Scripts.PlayModeTest
{
    public class TreatmentEfficacyHandlerTests
    {
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
    }
}