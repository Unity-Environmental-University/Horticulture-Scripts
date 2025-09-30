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
        private class TestAffliction : PlantAfflictions.IAffliction
        {
            private readonly bool _canTreat;
            private readonly string _name;

            public TestAffliction(string name, bool canTreat)
            {
                _name = name;
                _canTreat = canTreat;
            }

            public string Name => _name;
            public string Description => string.Empty;
            public Color Color => Color.white;
            public Shader Shader => null;
            public List<PlantAfflictions.ITreatment> AcceptableTreatments { get; } = new();

            public bool CanBeTreatedBy(PlantAfflictions.ITreatment treatment)
            {
                return _canTreat;
            }

            public void TreatWith(PlantAfflictions.ITreatment treatment, PlantController plant) { }

            public void TickDay(PlantController plant) { }

            public PlantAfflictions.IAffliction Clone()
            {
                return new TestAffliction(_name, _canTreat);
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

            var cache = (List<RelationalEfficacy>)storageField?.GetValue(handler);
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
    }
}
