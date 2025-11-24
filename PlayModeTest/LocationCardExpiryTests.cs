using System.Collections;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace _project.Scripts.PlayModeTest
{
    public class LocationCardExpiryTests
    {
        private PlacedCardHolder _cardHolder;
        private SpotDataHolder _spotDataHolder;
        private GameObject _spotObject;

        [SetUp]
        public void Setup()
        {
            // Setup CardGameMaster singleton dependencies
            var cgmObj = new GameObject("CardGameMaster");
            var cgm = cgmObj.AddComponent<CardGameMaster>();

            // Add required components to CGM or ensure they are accessible
            cgmObj.AddComponent<DeckManager>();
            cgmObj.AddComponent<ScoreManager>();
            cgmObj.AddComponent<TurnController>();

            // Force Awake/Start if necessary, but usually adding component does it in PlayMode tests if object is active.
            // CardGameMaster.Instance is set in Awake.

            _spotObject = new GameObject("Spot");
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

            if (_spotObject)
                Object.Destroy(_spotObject);
        }

        [UnityTest]
        public IEnumerator LocationCard_Expires_WithoutPlant()
        {
            // Arrange
            var locationCard = new UreaBasic(); // Duration 3
            // Ensure no plant is present
            Assert.IsNull(_spotObject.GetComponentInChildren<PlantController>());

            // Act
            _spotDataHolder.OnLocationCardPlaced(locationCard);
            _cardHolder.placedCard = locationCard;

            // Process turns equal to duration
            for (var i = 0; i < locationCard.EffectDuration; i++) _spotDataHolder.ProcessTurn();

            // Assert - Should still be there (duration is 3, so after 3 turns it should be 0 and expire? Or expire on next?)
            // Logic says: _remainingDuration--; if (_remainingDuration > 0) return;
            // So if duration is 3:
            // Turn 1: rem=2, return
            // Turn 2: rem=1, return
            // Turn 3: rem=0, expire

            // However, due to the bug, it returns early if no plant.

            // Let's check if it expired. If expired, cLocationCard should be null (or handled by holder)
            // But SpotDataHolder.cLocationCard is private. We can check via side effects or reflection, 
            // or just check if the holder was cleared.

            // The holder.ClearLocationCardByExpiry() clears holder.placedCard.

            yield return null;

            // If bug exists, card is still there
            Assert.IsNull(_cardHolder.placedCard, "Location card should have expired and been removed from holder");
        }
    }
}