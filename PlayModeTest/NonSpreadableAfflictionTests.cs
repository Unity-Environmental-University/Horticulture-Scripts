using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
    /// <summary>
    ///     Tests for non-spreadable afflictions (e.g., condition-type afflictions like Dehydrated).
    ///     Verifies that afflictions with IsSpreadable=false do not spread to neighboring plants.
    /// </summary>
    public class NonSpreadableAfflictionTests
    {
        private const int SpreadAttemptsHighConfidence = 50; // 50 attempts for high confidence testing
        private static MethodInfo _spreadAfflictionsMethod;

        private static void ResetCardGameMasterSingleton()
        {
            typeof(CardGameMaster)
                .GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(null, null);
        }

        private static MethodInfo GetSpreadAfflictionsMethod()
        {
            if (_spreadAfflictionsMethod != null) return _spreadAfflictionsMethod;
            _spreadAfflictionsMethod = typeof(TurnController).GetMethod(
                "SpreadAfflictions",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (_spreadAfflictionsMethod == null)
                Assert.Fail("Critical: SpreadAfflictions method not found.");

            return _spreadAfflictionsMethod;
        }

        private static PlantController CreatePlant()
        {
            var plantGo = new GameObject("Plant");
            var plant = plantGo.AddComponent<PlantController>();
            plant.PlantCard = new ColeusCard();
            plant.canSpreadAfflictions = true;
            plant.canReceiveAfflictions = true;
            return plant;
        }

        /// <summary>
        ///     Creates a minimal TurnController environment with required dependencies.
        /// </summary>
        private static (TurnController turnController, GameObject rootGo) CreateTurnControllerEnvironment()
        {
            var rootGo = new GameObject("TurnControllerRoot");
            rootGo.SetActive(false); // Keep inactive during setup

            // Add DeckManager (required by ScoreManager)
            var deckManager = rootGo.AddComponent<DeckManager>();
            deckManager.plantLocations = new List<PlantHolder>(); // Empty list

            // Add ScoreManager (required by SpreadAfflictions)
            var scoreManager = rootGo.AddComponent<ScoreManager>();

            // Add TurnController (disabled to prevent Start/Update from running)
            var turnController = rootGo.AddComponent<TurnController>();
            turnController.enabled = false;

            // Add CardGameMaster and wire up references
            var cardGameMaster = rootGo.AddComponent<CardGameMaster>();
            cardGameMaster.deckManager = deckManager;
            cardGameMaster.scoreManager = scoreManager;
            cardGameMaster.turnController = turnController;

            var treatmentTextGo = new GameObject("TreatmentCostText");
            treatmentTextGo.transform.SetParent(rootGo.transform);
            cardGameMaster.treatmentCostText = treatmentTextGo.AddComponent<TextMeshPro>();

            var potentialTextGo = new GameObject("PotentialProfitText");
            potentialTextGo.transform.SetParent(rootGo.transform);
            cardGameMaster.potentialProfitText = potentialTextGo.AddComponent<TextMeshPro>();

            var moneysTextGo = new GameObject("MoneysText");
            moneysTextGo.transform.SetParent(rootGo.transform);
            cardGameMaster.moneysText = moneysTextGo.AddComponent<TextMeshPro>();

            // Activate GameObject (Awake() will run and set CardGameMaster.Instance)
            // Ignore expected warnings about missing components
            LogAssert.ignoreFailingMessages = true;
            rootGo.SetActive(true);
            LogAssert.ignoreFailingMessages = false;

            var cinematicDirector = rootGo.AddComponent<CinematicDirector>();
            cardGameMaster.cinematicDirector = cinematicDirector;

            var soundSystemGo = new GameObject("SoundSystem");
            soundSystemGo.transform.SetParent(rootGo.transform);
            var soundSystem = soundSystemGo.AddComponent<SoundSystemMaster>();
            cardGameMaster.soundSystem = soundSystem;

            return (turnController, rootGo);
        }

        [UnityTest]
        public IEnumerator DehydratedAffliction_HasIsSpreadableFalse()
        {
            var dehydrated = new PlantAfflictions.DehydratedAffliction();

            Assert.IsFalse(dehydrated.IsSpreadable,
                "DehydratedAffliction should have IsSpreadable=false");

            yield return null;
        }

        [UnityTest]
        public IEnumerator NonSpreadableAffliction_DoesNotSpreadToNeighbors()
        {
            var (turnController, tcGo) = CreateTurnControllerEnvironment();

            // Setup three plants in a row
            var plants = new[] { CreatePlant(), CreatePlant(), CreatePlant() };

            // Add dehydrated affliction to middle plant
            var dehydrated = new PlantAfflictions.DehydratedAffliction();
            plants[1].AddAffliction(dehydrated);

            Assert.IsTrue(plants[1].HasAffliction(dehydrated),
                "Middle plant should have dehydrated affliction");
            Assert.IsFalse(plants[0].HasAffliction(dehydrated),
                "Left plant should not have affliction initially");
            Assert.IsFalse(plants[2].HasAffliction(dehydrated),
                "Right plant should not have affliction initially");

            var spreadMethod = GetSpreadAfflictionsMethod();

            // Try to spread many times - should never spread due to IsSpreadable=false
            for (var i = 0; i < SpreadAttemptsHighConfidence; i++)
                spreadMethod.Invoke(turnController, new object[] { plants });

            // Verify no spreading occurred
            Assert.IsFalse(plants[0].HasAffliction(dehydrated),
                "Dehydrated affliction should NOT spread to left neighbor");
            Assert.IsFalse(plants[2].HasAffliction(dehydrated),
                "Dehydrated affliction should NOT spread to right neighbor");

            // Cleanup
            foreach (var plant in plants)
                if (plant)
                    Object.Destroy(plant.gameObject);
            if (tcGo) Object.Destroy(tcGo);

            ResetCardGameMasterSingleton();

            yield return null;
        }

        [UnityTest]
        public IEnumerator NonSpreadableAffliction_CanStillBeManuallyAdded()
        {
            var (_, tcGo) = CreateTurnControllerEnvironment();
            var plant = CreatePlant();
            var dehydrated = new PlantAfflictions.DehydratedAffliction();

            // Manually add the affliction
            plant.AddAffliction(dehydrated);

            Assert.IsTrue(plant.HasAffliction(dehydrated),
                "Non-spreadable afflictions can still be manually added to plants");

            // Cleanup
            if (plant) Object.Destroy(plant.gameObject);
            if (tcGo) Object.Destroy(tcGo);
            ResetCardGameMasterSingleton();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExistingAfflictions_AreStillSpreadable()
        {
            var (turnController, tcGo) = CreateTurnControllerEnvironment();

            // Setup three plants
            var plants = new[] { CreatePlant(), CreatePlant(), CreatePlant() };

            // Add spreadable affliction (Aphids) to middle plant
            var aphids = new PlantAfflictions.AphidsAffliction();
            plants[1].AddAffliction(aphids);

            Assert.IsTrue(aphids.IsSpreadable, "Aphids should be spreadable");

            var spreadMethod = GetSpreadAfflictionsMethod();

            // Try to spread - should eventually succeed
            var spreadOccurred = false;
            for (var i = 0; i < SpreadAttemptsHighConfidence; i++)
            {
                spreadMethod.Invoke(turnController, new object[] { plants });
                if (!plants[0].HasAffliction(aphids) && !plants[2].HasAffliction(aphids)) continue;
                spreadOccurred = true;
                break;
            }

            Assert.IsTrue(spreadOccurred,
                "Spreadable afflictions (like Aphids) should still spread normally");

            // Cleanup
            foreach (var plant in plants)
                if (plant)
                    Object.Destroy(plant.gameObject);
            if (tcGo) Object.Destroy(tcGo);

            ResetCardGameMasterSingleton();

            yield return null;
        }

        [UnityTest]
        public IEnumerator DehydratedAffliction_CausesPlantValueReduction()
        {
            var (_, tcGo) = CreateTurnControllerEnvironment();
            var plant = CreatePlant();
            var initialValue = plant.PlantCard.Value ?? 0;

            var dehydrated = new PlantAfflictions.DehydratedAffliction();
            plant.AddAffliction(dehydrated);

            // Call TickDay to simulate turn progression
            dehydrated.TickDay(plant);

            var newValue = plant.PlantCard.Value ?? 0;
            Assert.Less(newValue, initialValue,
                "Dehydrated affliction should reduce plant value each turn");

            // Cleanup
            if (plant) Object.Destroy(plant.gameObject);
            if (tcGo) Object.Destroy(tcGo);
            ResetCardGameMasterSingleton();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllAfflictions_HaveDescriptionResource()
        {
            var afflictionTypes = typeof(PlantAfflictions)
                .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Where(t =>
                    typeof(PlantAfflictions.IAffliction).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in afflictionTypes)
            {
                var affliction = (PlantAfflictions.IAffliction)Activator.CreateInstance(type);
                var baseName = affliction.Name.Replace(" ", string.Empty);
                var hyphenName = Regex.Replace(baseName, "(?<!^)([A-Z])", "-$1");
                var resource = Resources.Load<TextAsset>($"Descriptions/{hyphenName}");
                Assert.IsNotNull(resource,
                    $"Missing Resources/Descriptions/{hyphenName}.txt for affliction {affliction.Name}");
            }

            ResetCardGameMasterSingleton();
            yield return null;
        }
    }
}