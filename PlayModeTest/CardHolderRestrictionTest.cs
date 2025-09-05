using _project.Scripts.Card_Core;
using NUnit.Framework;
using UnityEngine;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    /// Tests for card holder type restrictions functionality.
    /// </summary>
    public class CardHolderRestrictionTest
    {
        private GameObject _testGameObject;
        private PlacedCardHolder _placedCardHolder;
        
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
    }
}