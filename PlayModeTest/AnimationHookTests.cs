using _project.Scripts.Classes;
using _project.Scripts.Core;
using NUnit.Framework;
using UnityEngine;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    /// Tests for plant affliction animation hook system.
    /// Verifies that animation triggers are properly configured and PlantCard.Name-based animation prefixes work correctly.
    /// </summary>
    public class AnimationHookTests
    {
        [Test]
        public void DehydratedAffliction_HasCorrectAnimationTriggerNames()
        {
            // Arrange
            var affliction = new PlantAfflictions.DehydratedAffliction();

            // Assert
            Assert.AreEqual("Droop", affliction.AnimationTriggerName,
                "DehydratedAffliction should have 'Droop' animation trigger");
            Assert.AreEqual("Recover", affliction.RecoveryAnimationTriggerName,
                "DehydratedAffliction should have 'Recover' recovery animation trigger");
        }

        [Test]
        public void NeedsLightAffliction_HasCorrectAnimationTriggerNames()
        {
            // Arrange
            var affliction = new PlantAfflictions.NeedsLightAffliction();

            // Assert
            Assert.AreEqual("Wilt", affliction.AnimationTriggerName,
                "NeedsLightAffliction should have 'Wilt' animation trigger");
            Assert.AreEqual("Recover", affliction.RecoveryAnimationTriggerName,
                "NeedsLightAffliction should have 'Recover' recovery animation trigger");
        }

        [Test]
        public void ThripsAffliction_HasNoAnimationTriggers()
        {
            // Arrange
            PlantAfflictions.IAffliction affliction = new PlantAfflictions.ThripsAffliction();

            // Assert - default interface implementation returns null
            Assert.IsNull(affliction.AnimationTriggerName,
                "ThripsAffliction should have no animation trigger (uses particle effects instead)");
            Assert.IsNull(affliction.RecoveryAnimationTriggerName,
                "ThripsAffliction should have no recovery animation trigger");
        }

        [Test]
        public void MildewAffliction_HasNoAnimationTriggers()
        {
            // Arrange
            PlantAfflictions.IAffliction affliction = new PlantAfflictions.MildewAffliction();

            // Assert
            Assert.IsNull(affliction.AnimationTriggerName,
                "MildewAffliction should have no animation trigger (uses shader effects instead)");
            Assert.IsNull(affliction.RecoveryAnimationTriggerName,
                "MildewAffliction should have no recovery animation trigger");
        }

        [Test]
        public void PlantCard_Name_ProducesCorrectAnimationPrefix()
        {
            // Arrange
            var chrysanthemumCard = new ChrysanthemumCard();
            var coleusCard = new ColeusCard();
            var pepperCard = new PepperCard();
            var cucumberCard = new CucumberCard();

            // Act
            var chrysanthemumPrefix = chrysanthemumCard.Name.ToLower();
            var coleusPrefix = coleusCard.Name.ToLower();
            var pepperPrefix = pepperCard.Name.ToLower();
            var cucumberPrefix = cucumberCard.Name.ToLower();

            // Assert
            Assert.AreEqual("chrysanthemum", chrysanthemumPrefix, "Chrysanthemum card should produce 'chrysanthemum' prefix");
            Assert.AreEqual("coleus", coleusPrefix, "Coleus card should produce 'coleus' prefix");
            Assert.AreEqual("pepper", pepperPrefix, "Pepper card should produce 'pepper' prefix");
            Assert.AreEqual("cucumber", cucumberPrefix, "Cucumber card should produce 'cucumber' prefix");
        }

        [Test]
        public void PlantController_HasAnimatorParameter_ReturnsFalseWhenNoAnimator()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();

            // Act
            var hasParameter = (bool)plantController.GetType()
                .GetMethod("HasAnimatorParameter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(plantController, new object[] { "chrysanthemumDroop" });

            // Assert
            Assert.IsFalse(hasParameter, "Should return false when no animator is present");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AnimationTriggerNames_AreConsistentAcrossAfflictions()
        {
            // Arrange
            var dehydrated = new PlantAfflictions.DehydratedAffliction();
            var needsLight = new PlantAfflictions.NeedsLightAffliction();

            // Assert - Both should use the same recovery animation
            Assert.AreEqual(dehydrated.RecoveryAnimationTriggerName, needsLight.RecoveryAnimationTriggerName,
                "Both condition afflictions should use the same 'Recover' animation");
        }

        [Test]
        public void PlantController_AddAffliction_WithDehydration_DoesNotThrowException()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            plantController.PlantCard = new ChrysanthemumCard();
            var affliction = new PlantAfflictions.DehydratedAffliction();

            // Act & Assert - Should not throw even without animator
            Assert.DoesNotThrow(() => plantController.AddAffliction(affliction),
                "AddAffliction should handle missing animator gracefully");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void PlantController_RemoveAffliction_WithDehydration_DoesNotThrowException()
        {
            // Arrange
            var gameObject = new GameObject("TestPlant");
            var plantController = gameObject.AddComponent<PlantController>();
            plantController.PlantCard = new ColeusCard();
            var affliction = new PlantAfflictions.DehydratedAffliction();
            plantController.AddAffliction(affliction);

            // Act & Assert - Should not throw even without animator
            Assert.DoesNotThrow(() => plantController.RemoveAffliction(affliction),
                "RemoveAffliction should handle missing animator gracefully");

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void AllAfflictionTypes_HaveAnimationTriggerProperties()
        {
            // Arrange
            var afflictions = new PlantAfflictions.IAffliction[]
            {
                new PlantAfflictions.DehydratedAffliction(),
                new PlantAfflictions.NeedsLightAffliction(),
                new PlantAfflictions.ThripsAffliction(),
                new PlantAfflictions.MildewAffliction(),
                new PlantAfflictions.AphidsAffliction(),
                new PlantAfflictions.SpiderMitesAffliction(),
                new PlantAfflictions.FungusGnatsAffliction(),
                new PlantAfflictions.MealyBugsAffliction()
            };

            // Act & Assert - All should have the properties (even if they return null)
            foreach (var affliction in afflictions)
            {
                Assert.DoesNotThrow(() =>
                {
                    var trigger = affliction.AnimationTriggerName;
                    var recovery = affliction.RecoveryAnimationTriggerName;
                }, $"{affliction.Name} should have animation trigger properties accessible");
            }
        }
    }
}
