using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _project.Scripts.Audio;
using _project.Scripts.Card_Core;
using _project.Scripts.Cinematics;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace _project.Scripts.PlayModeTest
{
    public class IsolationCardTests
    {
        // Multiple attempts needed due to 50% random spread chance in TurnController.SpreadAfflictions
        private const int SpreadAttemptsStandard = 10; // Covers ~99% probability for single spread
        private const int SpreadAttemptsHighConfidence = 50; // Covers ~99.999% probability

        private static MethodInfo _spreadAfflictionsMethod;

        /// <summary>
        ///     Gets the SpreadAfflictions method via reflection with caching.
        /// </summary>
        private static MethodInfo GetSpreadAfflictionsMethod()
        {
            if (_spreadAfflictionsMethod != null) return _spreadAfflictionsMethod;
            _spreadAfflictionsMethod = typeof(TurnController).GetMethod(
                "SpreadAfflictions",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (_spreadAfflictionsMethod == null)
                Assert.Fail("Critical: SpreadAfflictions method not found. " +
                            "Test infrastructure needs updating if TurnController API changed.");

            return _spreadAfflictionsMethod;
        }

        private static (GameObject root, SpotDataHolder spot, PlantController plant) CreateSpotWithPlant()
        {
            var spotRoot = new GameObject("Spot");
            var spot = spotRoot.AddComponent<SpotDataHolder>();

            var plantGo = new GameObject("Plant");
            plantGo.transform.SetParent(spotRoot.transform);
            var plant = plantGo.AddComponent<PlantController>();
            plant.PlantCard = new ColeusCard();
            plant.canSpreadAfflictions = true;
            plant.canReceiveAfflictions = true;

            return (spotRoot, spot, plant);
        }

        private static IEnumerator Cleanup(GameObject root)
        {
            if (root) Object.Destroy(root);
            yield return null;
        }

        /// <summary>
        ///     Comprehensive cleanup for integration test environments.
        /// </summary>
        private static IEnumerator CleanupTestEnvironment(List<GameObject> roots)
        {
            // Destroy all GameObjects
            foreach (var root in roots.Where(root => root))
                Object.Destroy(root);

            // Reset CardGameMaster singleton state
            typeof(CardGameMaster)
                .GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(null, null);

            // Reset LogAssert state to prevent test pollution
            LogAssert.ignoreFailingMessages = false;

            // Wait multiple frames for Unity to complete cleanup
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnLocationCardPlaced_DisablesAfflictionTransfer()
        {
            var (root, spot, plant) = CreateSpotWithPlant();
            yield return null;

            var isolation = new IsolateBasic();

            spot.OnLocationCardPlaced(isolation);
            yield return null;

            Assert.IsFalse(plant.canSpreadAfflictions,
                "Isolation should prevent the plant from spreading afflictions.");
            Assert.IsFalse(plant.canReceiveAfflictions,
                "Isolation should prevent the plant from receiving new afflictions.");

            yield return Cleanup(root);
        }

        [UnityTest]
        public IEnumerator OnLocationCardRemoved_ReenablesAfflictionTransfer()
        {
            var (root, spot, plant) = CreateSpotWithPlant();
            yield return null;

            var isolation = new IsolateBasic();
            spot.OnLocationCardPlaced(isolation);
            yield return null;

            spot.OnLocationCardRemoved();
            yield return null;

            Assert.IsTrue(plant.canSpreadAfflictions,
                "Removing isolation should allow the plant to spread afflictions again.");
            Assert.IsTrue(plant.canReceiveAfflictions,
                "Removing isolation should allow the plant to receive afflictions again.");

            yield return Cleanup(root);
        }

        [UnityTest]
        public IEnumerator ProcessTurn_ExpiryRestoresAfflictionTransfer()
        {
            var (root, spot, plant) = CreateSpotWithPlant();
            yield return null;

            var isolation = new IsolateBasic();
            spot.OnLocationCardPlaced(isolation);
            yield return null;

            Assert.IsFalse(plant.canSpreadAfflictions);
            Assert.IsFalse(plant.canReceiveAfflictions);

            for (var turn = 0; turn < isolation.EffectDuration; turn++)
            {
                spot.ProcessTurn();
                spot.FinalizeLocationCardTurn();
                yield return null;
            }

            Assert.IsTrue(plant.canSpreadAfflictions, "Isolation expiry should restore spreading of afflictions.");
            Assert.IsTrue(plant.canReceiveAfflictions, "Isolation expiry should restore receiving of afflictions.");

            yield return Cleanup(root);
        }

        /// <summary>
        ///     Tests that an isolated plant with an affliction does NOT spread it to neighboring plants.
        /// </summary>
        [UnityTest]
        public IEnumerator IsolatedPlant_DoesNotSpreadAfflictionsToNeighbors()
        {
            // Setup TurnController environment
            var (turnController, plants, roots) = SetupTurnControllerWithPlants(3);
            yield return null;

            // Give middle plant an affliction
            plants[1].AddAffliction(new PlantAfflictions.ThripsAffliction());

            // Apply IsolateBasic to middle plant via its spot
            var middleSpot = plants[1].GetComponentInParent<SpotDataHolder>();
            middleSpot.OnLocationCardPlaced(new IsolateBasic());
            yield return null;

            // Verify isolation is applied
            Assert.IsFalse(plants[1].canSpreadAfflictions,
                "Middle plant should have spreading disabled immediately after IsolateBasic placement");
            Assert.IsTrue(plants[1].CurrentAfflictions.Count > 0,
                "Middle plant should have affliction before spread attempt");

            // Get SpreadAfflictions method via reflection
            var spreadMethod = GetSpreadAfflictionsMethod();

            // Call SpreadAfflictions multiple times to account for randomness
            for (var i = 0; i < SpreadAttemptsStandard; i++)
            {
                spreadMethod.Invoke(turnController, new object[] { plants });
                yield return null;
            }

            // Assert: Neighbors should NOT have received the affliction
            Assert.AreEqual(0, plants[0].CurrentAfflictions.Count,
                "First plant should not have received affliction from isolated neighbor " +
                $"after {SpreadAttemptsStandard} spread attempts");
            Assert.AreEqual(0, plants[2].CurrentAfflictions.Count,
                "Third plant should not have received affliction from isolated neighbor " +
                $"after {SpreadAttemptsStandard} spread attempts");

            // Cleanup
            yield return CleanupTestEnvironment(roots);
        }

        /// <summary>
        ///     Tests that an isolated plant does NOT receive afflictions from neighboring infected plants.
        /// </summary>
        [UnityTest]
        public IEnumerator IsolatedPlant_DoesNotReceiveAfflictionsFromNeighbors()
        {
            // Setup TurnController environment
            var (turnController, plants, roots) = SetupTurnControllerWithPlants(3);
            yield return null;

            // Give first plant an affliction (no isolation)
            plants[0].AddAffliction(new PlantAfflictions.ThripsAffliction());

            // Apply IsolateBasic to middle plant (potential target)
            var middleSpot = plants[1].GetComponentInParent<SpotDataHolder>();
            middleSpot.OnLocationCardPlaced(new IsolateBasic());
            yield return null;

            // Verify isolation is applied to middle plant
            Assert.IsFalse(plants[1].canReceiveAfflictions,
                "Middle plant should have receiving disabled immediately after IsolateBasic placement");
            Assert.AreEqual(0, plants[1].CurrentAfflictions.Count,
                "Middle plant should start with no afflictions before spread attempt");

            // Get SpreadAfflictions method via reflection
            var spreadMethod = GetSpreadAfflictionsMethod();

            // Call SpreadAfflictions multiple times to account for randomness
            for (var i = 0; i < SpreadAttemptsStandard; i++)
            {
                spreadMethod.Invoke(turnController, new object[] { plants });
                yield return null;
            }

            // Assert: Isolated middle plant should NOT have received affliction
            Assert.AreEqual(0, plants[1].CurrentAfflictions.Count,
                "Isolated plant should not have received affliction from neighbor " +
                $"after {SpreadAttemptsStandard} spread attempts");

            // Note: We don't verify third plant state as it depends on multiple random factors
            // (spread chance, affliction selection, target selection) and isn't relevant to this test's goal

            // Cleanup
            yield return CleanupTestEnvironment(roots);
        }

        /// <summary>
        ///     Tests that an isolated plant remains protected during the spread phase of its final turn,
        ///     even after ProcessTurn() has marked it for expiry.
        ///     This verifies the two-phase commit behavior prevents vulnerability during expiry.
        /// </summary>
        [UnityTest]
        public IEnumerator IsolationCard_ProtectsDuringFinalTurn_BeforeFinalization()
        {
            // Setup TurnController environment
            var (turnController, plants, roots) = SetupTurnControllerWithPlants(3);
            yield return null;

            // Give first plant an affliction (source)
            plants[0].AddAffliction(new PlantAfflictions.ThripsAffliction());

            // Apply IsolateBasic to middle plant (potential target)
            var middleSpot = plants[1].GetComponentInParent<SpotDataHolder>();
            var isolation = new IsolateBasic();
            middleSpot.OnLocationCardPlaced(isolation);
            yield return null;

            // Verify isolation is applied
            Assert.IsFalse(plants[1].canReceiveAfflictions,
                "Middle plant should have receiving disabled after IsolateBasic placement");

            // Process turns up to EffectDuration - 1 (one turn before expiry)
            for (var turn = 0; turn < isolation.EffectDuration - 1; turn++)
            {
                middleSpot.ProcessTurn();
                middleSpot.FinalizeLocationCardTurn();
                yield return null;
            }

            // Verify isolation is still active
            Assert.IsFalse(plants[1].canReceiveAfflictions,
                "Middle plant should still be protected before final turn");

            // NOW: Simulate the FINAL turn's critical sequence
            // Step 1: ProcessTurn() marks for expiry but keeps effect active
            middleSpot.ProcessTurn();
            yield return null;

            // Verify protection is STILL active (this is the critical assertion)
            Assert.IsFalse(plants[1].canReceiveAfflictions,
                "After ProcessTurn() on final turn, plant should STILL be protected during spread phase");

            // Step 2: Spread phase happens (before finalization)
            var spreadMethod = GetSpreadAfflictionsMethod();
            for (var i = 0; i < SpreadAttemptsStandard; i++)
            {
                spreadMethod.Invoke(turnController, new object[] { plants });
                yield return null;
            }

            // Assert: Isolated middle plant should NOT have received affliction during spread
            Assert.AreEqual(0, plants[1].CurrentAfflictions.Count,
                "Isolated plant should remain protected during spread phase of final turn " +
                $"(after ProcessTurn, before FinalizeLocationCardTurn). Attempted {SpreadAttemptsStandard} spreads.");

            // Step 3: FinalizeLocationCardTurn() removes the effect
            middleSpot.FinalizeLocationCardTurn();
            yield return null;

            // Verify protection is NOW disabled
            Assert.IsTrue(plants[1].canReceiveAfflictions,
                "After FinalizeLocationCardTurn(), plant should no longer be protected");
            Assert.IsTrue(plants[1].canSpreadAfflictions,
                "After FinalizeLocationCardTurn(), plant should be able to spread again");

            // Cleanup
            yield return CleanupTestEnvironment(roots);
        }

        /// <summary>
        ///     Tests that spreading resumes after IsolateBasic expires.
        /// </summary>
        [UnityTest]
        public IEnumerator IsolatedPlant_ResumesSpreadingAfterExpiry()
        {
            // Setup TurnController environment
            var (turnController, plants, roots) = SetupTurnControllerWithPlants(3);
            yield return null;

            // Give middle plant an affliction
            plants[1].AddAffliction(new PlantAfflictions.ThripsAffliction());

            // Apply IsolateBasic to middle plant
            var middleSpot = plants[1].GetComponentInParent<SpotDataHolder>();
            var isolation = new IsolateBasic();
            middleSpot.OnLocationCardPlaced(isolation);
            yield return null;

            // Verify isolation is active
            Assert.IsFalse(plants[1].canSpreadAfflictions, "Isolation should be active");

            // Process turns until expiry
            for (var turn = 0; turn < isolation.EffectDuration; turn++)
            {
                middleSpot.ProcessTurn();
                middleSpot.FinalizeLocationCardTurn();
                yield return null;
            }

            // Verify spreading is re-enabled
            Assert.IsTrue(plants[1].canSpreadAfflictions,
                "Spreading should be re-enabled after IsolateBasic expiry");
            Assert.IsTrue(plants[1].canReceiveAfflictions,
                "Receiving should be re-enabled after IsolateBasic expiry");

            // Get SpreadAfflictions method via reflection
            var spreadMethod = GetSpreadAfflictionsMethod();

            // Call SpreadAfflictions multiple times to increase likelihood of spread
            var didSpread = false;
            var attemptCount = 0;
            for (var i = 0; i < SpreadAttemptsHighConfidence; i++)
            {
                spreadMethod.Invoke(turnController, new object[] { plants });
                yield return null;
                attemptCount++;

                if (plants[0].CurrentAfflictions.Count <= 0 && plants[2].CurrentAfflictions.Count <= 0) continue;
                didSpread = true;
                break;
            }

            // Assert: At least one neighbor should have received the affliction
            Assert.IsTrue(didSpread,
                "After isolation expiry, affliction should eventually spread to neighbors. " +
                $"Attempted {attemptCount} spread cycles. Plant states: " +
                $"Plant[0] afflictions={plants[0].CurrentAfflictions.Count}, " +
                $"Plant[1] afflictions={plants[1].CurrentAfflictions.Count}, " +
                $"Plant[2] afflictions={plants[2].CurrentAfflictions.Count}");

            // Cleanup
            yield return CleanupTestEnvironment(roots);
        }

        /// <summary>
        ///     Helper method to set up a TurnController environment with multiple plants.
        /// </summary>
        private static (TurnController turnController, PlantController[] plants, List<GameObject> roots)
            SetupTurnControllerWithPlants(int plantCount)
        {
            var roots = new List<GameObject>();

            try
            {
                // Create CardGameMaster environment
                var cardGameMasterGo = new GameObject("CardGameMaster");
                cardGameMasterGo.SetActive(false);
                roots.Add(cardGameMasterGo);

                var deckManager = cardGameMasterGo.AddComponent<DeckManager>();
                var scoreManager = cardGameMasterGo.AddComponent<ScoreManager>();
                var turnController = cardGameMasterGo.AddComponent<TurnController>();
                var cardGameMaster = cardGameMasterGo.AddComponent<CardGameMaster>();
                var soundSystem = cardGameMasterGo.AddComponent<SoundSystemMaster>();
                var audioSource = cardGameMasterGo.AddComponent<AudioSource>();
                cardGameMasterGo.AddComponent<AudioListener>();
                cardGameMasterGo.AddComponent<CinematicDirector>();

                // Validate critical components
                Assert.IsNotNull(cardGameMaster, "Failed to create CardGameMaster component");
                Assert.IsNotNull(turnController, "Failed to create TurnController component");
                Assert.IsNotNull(deckManager, "Failed to create DeckManager component");

                // Setup minimal TurnController dependencies
                var lostObjectsGo = new GameObject("LostObjects");
                var winScreenGo = new GameObject("WinScreen");
                turnController.lostGameObjects = lostObjectsGo;
                turnController.winScreen = winScreenGo;
                roots.Add(lostObjectsGo);
                roots.Add(winScreenGo);

                // Setup DeckManager
                var plantLocations = new List<Transform>();
                var actionParentGo = new GameObject("ActionCardParent");
                deckManager.actionCardParent = actionParentGo.transform;
                roots.Add(actionParentGo);

                // Create mock UI for ScoreManager
                var treatmentCostTextGo = new GameObject("TreatmentCostText");
                var treatmentCostText = treatmentCostTextGo.AddComponent<TextMeshPro>();
                var potentialProfitTextGo = new GameObject("PotentialProfitText");
                var potentialProfitText = potentialProfitTextGo.AddComponent<TextMeshPro>();
                roots.Add(treatmentCostTextGo);
                roots.Add(potentialProfitTextGo);

                // Inject dependencies
                cardGameMaster.deckManager = deckManager;
                cardGameMaster.scoreManager = scoreManager;
                cardGameMaster.turnController = turnController;
                cardGameMaster.soundSystem = soundSystem;
                cardGameMaster.playerHandAudioSource = audioSource;
                cardGameMaster.treatmentCostText = treatmentCostText;
                cardGameMaster.potentialProfitText = potentialProfitText;

                // Set CardGameMaster instance via reflection
                typeof(CardGameMaster)
                    .GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.SetValue(null, cardGameMaster);

                // Create plants with spots
                var plants = new PlantController[plantCount];
                for (var i = 0; i < plantCount; i++)
                {
                    var spotRoot = new GameObject($"Spot_{i}");
                    var spot = spotRoot.AddComponent<SpotDataHolder>();
                    roots.Add(spotRoot);

                    var plantGo = new GameObject($"Plant_{i}");
                    plantGo.transform.SetParent(spotRoot.transform);
                    var plant = plantGo.AddComponent<PlantController>();
                    plant.PlantCard = new ColeusCard();
                    plant.canSpreadAfflictions = true;
                    plant.canReceiveAfflictions = true;

                    var plantFunctions = plantGo.AddComponent<PlantCardFunctions>();
                    plantFunctions.plantController = plant;
                    plantFunctions.deckManager = deckManager;
                    plant.plantCardFunctions = plantFunctions;

                    plants[i] = plant;
                    plantLocations.Add(spotRoot.transform);
                }

                deckManager.plantLocations = plantLocations;

                // Activate CardGameMaster (ignore expected setup warnings).
                // TurnController.Start kicks off the full turn loop which clears all plants; disable the
                // component ahead of activation so our manually-seeded plants remain in place.
                LogAssert.ignoreFailingMessages = true;
                turnController.enabled = false;
                cardGameMasterGo.SetActive(true);
                LogAssert.ignoreFailingMessages = false; // Reset immediately to prevent test pollution

                return (turnController, plants, roots);
            }
            catch (Exception ex)
            {
                // Cleanup on failure
                foreach (var root in roots.Where(root => root)) Object.Destroy(root);
                Assert.Fail($"Test environment setup failed: {ex.Message}");
                throw; // Unreachable but satisfies compiler
            }
        }
    }
}