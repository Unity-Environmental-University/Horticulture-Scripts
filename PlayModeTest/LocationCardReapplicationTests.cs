using System.Collections;
using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    /// Tests that verify location card effects re-apply when plants are replaced
    /// at the same location (e.g., during round transitions).
    /// </summary>
    public class LocationCardReapplicationTests
    {
        private PlacedCardHolder _cardHolder;
        private SpotDataHolder _spotDataHolder;
        private GameObject _spotObject;
        private GameObject _plantLocationRoot;

        [SetUp]
        public void Setup()
        {
            // Setup CardGameMaster singleton dependencies
            var cgmObj = new GameObject("CardGameMaster");
            cgmObj.AddComponent<CardGameMaster>();
            cgmObj.AddComponent<DeckManager>();
            cgmObj.AddComponent<ScoreManager>();
            cgmObj.AddComponent<TurnController>();

            // Create plant location hierarchy matching game structure
            _plantLocationRoot = new GameObject("PlantLocation");
            _spotObject = new GameObject("Spot");
            _spotObject.transform.SetParent(_plantLocationRoot.transform);
            _spotDataHolder = _spotObject.AddComponent<SpotDataHolder>();

            var holderObj = new GameObject("CardHolder");
            holderObj.transform.SetParent(_spotObject.transform);
            _cardHolder = holderObj.AddComponent<PlacedCardHolder>();

            _spotDataHolder.RegisterCardHolder(_cardHolder);
        }

        [TearDown]
        public void Teardown()
        {
            if (CardGameMaster.Instance)
                Object.Destroy(CardGameMaster.Instance.gameObject);

            if (_plantLocationRoot)
                Object.Destroy(_plantLocationRoot);
        }

        [UnityTest]
        public IEnumerator LadyBugs_ReapplyToNewPlant_WhenPlantReplaced()
        {
            // Arrange: Create initial plant
            var plant1 = CreatePlantAtLocation();
            yield return null;

            // Place LadyBugs location card
            var ladyBugsCard = new LadyBugsCard();
            _spotDataHolder.OnLocationCardPlaced(ladyBugsCard);
            _cardHolder.placedCard = ladyBugsCard;
            yield return null;

            // Verify initial plant has treatment
            Assert.IsTrue(plant1.CurrentTreatments.Any(t => t is PlantAfflictions.LadyBugs),
                "Initial plant should have LadyBugs treatment after card placement");

            // Act: Simulate plant replacement (as happens during round transition)
            Object.Destroy(plant1.gameObject);
            yield return null;

            var plant2 = CreatePlantAtLocation();
            _spotDataHolder.InvalidatePlantCache();
            _spotDataHolder.RefreshAssociatedPlant();
            yield return null;

            // Assert: New plant should have treatment re-applied
            Assert.IsTrue(plant2.CurrentTreatments.Any(t => t is PlantAfflictions.LadyBugs),
                "New plant should have LadyBugs treatment re-applied after RefreshAssociatedPlant");
        }

        [UnityTest]
        public IEnumerator LocationCard_ReappliesOnlyWhenActive()
        {
            // Arrange: Create plant and place location card
            var plant1 = CreatePlantAtLocation();
            yield return null;

            var locationCard = new UreaBasic(); // Non-permanent, expires after duration
            _spotDataHolder.OnLocationCardPlaced(locationCard);
            _cardHolder.placedCard = locationCard;
            yield return null;

            // Process turns until card expires
            for (var i = 0; i < locationCard.EffectDuration; i++)
            {
                _spotDataHolder.ProcessTurn();
                _spotDataHolder.FinalizeLocationCardTurn();
            }
            yield return null;

            // Act: Replace plant after card expired
            Object.Destroy(plant1.gameObject);
            yield return null;

            var plant2 = CreatePlantAtLocation();
            var treatmentCountBefore = plant2.CurrentTreatments.Count;

            _spotDataHolder.InvalidatePlantCache();
            _spotDataHolder.RefreshAssociatedPlant();
            yield return null;

            // Assert: Expired card should NOT re-apply
            Assert.AreEqual(treatmentCountBefore, plant2.CurrentTreatments.Count,
                "Expired location card should not re-apply to new plant");
        }

        [UnityTest]
        public IEnumerator MultipleLocationCards_AllReapplyToNewPlant()
        {
            // Arrange: Create plant
            var plant1 = CreatePlantAtLocation();
            yield return null;

            // Place first location card
            var ladyBugsCard = new LadyBugsCard();
            _spotDataHolder.OnLocationCardPlaced(ladyBugsCard);
            yield return null;

            // Act: Replace plant
            Object.Destroy(plant1.gameObject);
            yield return null;

            var plant2 = CreatePlantAtLocation();
            _spotDataHolder.InvalidatePlantCache();
            _spotDataHolder.RefreshAssociatedPlant();
            yield return null;

            // Assert: Treatment should be present
            Assert.IsTrue(plant2.CurrentTreatments.Any(t => t is PlantAfflictions.LadyBugs),
                "LadyBugs treatment should be re-applied to new plant");
        }

        [UnityTest]
        public IEnumerator NoLocationCard_RefreshDoesNotError()
        {
            // Arrange: Create plant without location card
            var plant1 = CreatePlantAtLocation();
            yield return null;

            // Act: Replace plant with no location card present
            Object.Destroy(plant1.gameObject);
            yield return null;

            var plant2 = CreatePlantAtLocation();

            // Should not throw error
            Assert.DoesNotThrow(() =>
            {
                _spotDataHolder.InvalidatePlantCache();
                _spotDataHolder.RefreshAssociatedPlant();
            });
            yield return null;

            // Assert: Plant should exist and have no errors
            Assert.IsNotNull(plant2);
            Assert.AreEqual(0, plant2.CurrentTreatments.Count);
        }

        /// <summary>
        /// Helper method to create a plant at the test location
        /// </summary>
        private PlantController CreatePlantAtLocation()
        {
            var plantObj = new GameObject("Plant");
            plantObj.transform.SetParent(_plantLocationRoot.transform);

            var plantController = plantObj.AddComponent<PlantController>();
            var plantCard = new ColeusCard();
            plantController.PlantCard = plantCard;

            return plantController;
        }
    }
}
