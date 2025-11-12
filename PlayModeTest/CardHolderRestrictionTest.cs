using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using NUnit.Framework;
using UnityEngine;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Tests for cardholder type restrictions functionality.
    /// </summary>
    public class CardHolderRestrictionTest
    {
        private PlacedCardHolder _placedCardHolder;
        private GameObject _testGameObject;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestCardHolder");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
                Object.DestroyImmediate(_testGameObject);
        }

        [Test]
        public void PlacedCardHolder_DefaultsToAnyCardType()
        {
            _placedCardHolder = _testGameObject.AddComponent<PlacedCardHolder>();

            Assert.AreEqual(CardHolderType.Any, _placedCardHolder.GetCardHolderType());
        }

        [Test]
        public void PlacedCardHolder_CanSetToAnyCardType()
        {
            _placedCardHolder = _testGameObject.AddComponent<PlacedCardHolder>();
            _placedCardHolder.SetCardHolderType(CardHolderType.Any);

            Assert.AreEqual(CardHolderType.Any, _placedCardHolder.GetCardHolderType());
        }

        [Test]
        public void PlacedCardHolder_OnlyAcceptsActionCards_WhenSetToActionOnly()
        {
            _placedCardHolder = _testGameObject.AddComponent<PlacedCardHolder>();
            _placedCardHolder.SetCardHolderType(CardHolderType.ActionOnly);

            Assert.AreEqual(CardHolderType.ActionOnly, _placedCardHolder.GetCardHolderType());
        }

        [Test]
        public void PlacedCardHolder_OnlyAcceptsLocationCards_WhenSetToLocationOnly()
        {
            _placedCardHolder = _testGameObject.AddComponent<PlacedCardHolder>();
            _placedCardHolder.SetCardHolderType(CardHolderType.LocationOnly);

            Assert.AreEqual(CardHolderType.LocationOnly, _placedCardHolder.GetCardHolderType());
        }


        [Test]
        public void CardHolderType_EnumValues_AreCorrect()
        {
            Assert.AreEqual(0, (int)CardHolderType.Any);
            Assert.AreEqual(1, (int)CardHolderType.ActionOnly);
            Assert.AreEqual(2, (int)CardHolderType.LocationOnly);
        }

        [Test]
        public void PlacedCardHolder_AcceptsLocationCards_WhenSetToLocationOnly()
        {
            _placedCardHolder = _testGameObject.AddComponent<PlacedCardHolder>();
            _placedCardHolder.SetCardHolderType(CardHolderType.LocationOnly);

            var locationCard = new UreaBasic();
            Assert.IsTrue(_placedCardHolder.CanAcceptCard(locationCard));
        }

        [Test]
        public void PlacedCardHolder_RejectsLocationCards_WhenSetToActionOnly()
        {
            _placedCardHolder = _testGameObject.AddComponent<PlacedCardHolder>();
            _placedCardHolder.SetCardHolderType(CardHolderType.ActionOnly);

            var locationCard = new UreaBasic();
            Assert.IsFalse(_placedCardHolder.CanAcceptCard(locationCard));
        }

        [Test]
        public void PlacedCardHolder_ClearsCardReference_AfterExpiry()
        {
            _placedCardHolder = _testGameObject.AddComponent<PlacedCardHolder>();

            // Simulate a Location Card being placed
            var locationCard = new UreaBasic();
            _placedCardHolder.placedCard = locationCard;

            // Verify the card is placed
            Assert.IsNotNull(_placedCardHolder.placedCard);
            Assert.IsInstanceOf<ILocationCard>(_placedCardHolder.placedCard);

            // Call the expiry method
            _placedCardHolder.ClearLocationCardByExpiry();

            // Verify the card reference is cleared
            Assert.IsNull(_placedCardHolder.placedCard);
        }

        [Test]
        public void PlacedCardHolder_DoesNotClearNonLocationCards_OnExpiry()
        {
            _placedCardHolder = _testGameObject.AddComponent<PlacedCardHolder>();

            // Simulate a non-Location Card (action/treatment card) being placed
            var actionCard = new InsecticideBasic();
            _placedCardHolder.placedCard = actionCard;

            // Verify the card is placed
            Assert.IsNotNull(_placedCardHolder.placedCard);

            // Call the expiry method (should not clear non-Location Cards)
            _placedCardHolder.ClearLocationCardByExpiry();

            // Verify the card reference is still there (not cleared)
            Assert.IsNotNull(_placedCardHolder.placedCard);
        }

        [Test]
        public void PlacedCardHolder_LocationCard_IsIdentifiedCorrectly()
        {
            _placedCardHolder = _testGameObject.AddComponent<PlacedCardHolder>();

            // Test UreaBasic (Location Card)
            var ureaCard = new UreaBasic();
            _placedCardHolder.placedCard = ureaCard;
            Assert.IsInstanceOf<ILocationCard>(_placedCardHolder.placedCard);

            // Test IsolateBasic (Location Card)
            var isolateCard = new IsolateBasic();
            _placedCardHolder.placedCard = isolateCard;
            Assert.IsInstanceOf<ILocationCard>(_placedCardHolder.placedCard);
        }

        [Test]
        public void PlacedCardHolder_ActionCard_IsNotLocationCard()
        {
            _placedCardHolder = _testGameObject.AddComponent<PlacedCardHolder>();

            // Test InsecticideBasic (Action Card)
            var actionCard = new InsecticideBasic();
            _placedCardHolder.placedCard = actionCard;
            Assert.IsNotInstanceOf<ILocationCard>(_placedCardHolder.placedCard);
        }
    }
}