using System;
using _project.Scripts.ModLoading;
using NUnit.Framework;
using UnityEngine;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Tests for the new ModTreatment system
    /// </summary>
    public class ModTreatmentTest
    {
        [Test]
        public void ModTreatment_GetEffectivenessFor_ReturnsCorrectValues()
        {
            // Create effectiveness array
            var effectiveness = new[]
            {
                new ModLoader.AfflictionEffectiveness { affliction = "Aphids", infectCure = 5, eggCure = 3 },
                new ModLoader.AfflictionEffectiveness { affliction = "SpiderMites", infectCure = 2, eggCure = 1 }
            };

            var treatment = new ModLoader.ModTreatment("Test Treatment", "A test treatment", effectiveness, true);

            // Test known affliction
            var (infectCure, eggCure) = treatment.GetEffectivenessFor("Aphids");
            Assert.AreEqual(5, infectCure);
            Assert.AreEqual(3, eggCure);

            // Test another known affliction
            (infectCure, eggCure) = treatment.GetEffectivenessFor("SpiderMites");
            Assert.AreEqual(2, infectCure);
            Assert.AreEqual(1, eggCure);

            // Test unknown affliction
            (infectCure, eggCure) = treatment.GetEffectivenessFor("UnknownPest");
            Assert.AreEqual(0, infectCure);
            Assert.AreEqual(0, eggCure);
        }

        [Test]
        public void ModTreatment_WithNullEffectiveness_HandlesGracefully()
        {
            var treatment = new ModLoader.ModTreatment("Test", "Test", null, true);

            var (infectCure, eggCure) = treatment.GetEffectivenessFor("AnyPest");
            Assert.AreEqual(0, infectCure);
            Assert.AreEqual(0, eggCure);
        }

        [Test]
        public void ModTreatment_WithEmptyEffectiveness_HandlesGracefully()
        {
            var effectiveness = Array.Empty<ModLoader.AfflictionEffectiveness>();
            var treatment = new ModLoader.ModTreatment("Test", "Test", effectiveness, true);

            var (infectCure, eggCure) = treatment.GetEffectivenessFor("AnyPest");
            Assert.AreEqual(0, infectCure);
            Assert.AreEqual(0, eggCure);
        }

        [Test]
        public void ModTreatment_BasicProperties_Work()
        {
            var effectiveness = new[]
            {
                new ModLoader.AfflictionEffectiveness { affliction = "Test", infectCure = 1, eggCure = 1 }
            };

            var treatment = new ModLoader.ModTreatment("My Treatment", "My Description", effectiveness, true);

            Assert.AreEqual("My Treatment", treatment.Name);
            Assert.AreEqual("My Description", treatment.Description);
            Assert.AreEqual(0, treatment.InfectCureValue); // Default fallback
            Assert.AreEqual(0, treatment.EggCureValue); // Default fallback
        }

        [Test]
        public void ModAffliction_BasicProperties_Work()
        {
            var affliction = new ModAffliction("TestPest", "A test pest", Color.red);

            Assert.AreEqual("TestPest", affliction.Name);
            Assert.AreEqual("A test pest", affliction.Description);
            Assert.AreEqual(Color.red, affliction.Color);
            Assert.IsNull(affliction.Shader);
            Assert.IsNull(affliction.GetCard());
        }

        [Test]
        public void ModAffliction_Clone_CreatesNewInstance()
        {
            var original = new ModAffliction("TestPest", "A test pest", Color.blue);
            var clone = original.Clone();

            Assert.IsNotNull(clone);
            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.Name, clone.Name);
            Assert.AreEqual(original.Description, clone.Description);
            Assert.AreEqual(original.Color, clone.Color);
        }
    }
}