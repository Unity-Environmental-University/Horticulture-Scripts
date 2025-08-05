using System.Collections.Generic;
using _project.Scripts.Classes;
using _project.Scripts.Stickers;
using NUnit.Framework;
using UnityEngine;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Tests custom sticker functionality, e.g., ValueReducerStickerDefinition.
    /// </summary>
    public class StickerTester
    {
        [Test]
        public void ValueReducerSticker_ReducesPositiveCardValue()
        {
            // Arrange
            var reducer = ScriptableObject.CreateInstance<ValueReducerSticker>();
            reducer.reductionAmount = 4;
            var card = new DummyCard(10);

            // Act
            reducer.Apply(card);

            // Assert: reduced by 4
            Assert.AreEqual(6, card.Value);
        }

        [Test]
        public void ValueReducerSticker_ReducesNegativeCardValueTowardsZero()
        {
            var reducer = ScriptableObject.CreateInstance<ValueReducerSticker>();
            reducer.reductionAmount = 4;
            var card = new DummyCard(-10);

            reducer.Apply(card);

            // Negative values move toward zero
            Assert.AreEqual(-6, card.Value);
        }

        [Test]
        public void ValueReducerSticker_DoesNothingOnZeroValue()
        {
            var reducer = ScriptableObject.CreateInstance<ValueReducerSticker>();
            reducer.reductionAmount = 4;
            var card = new DummyCard(0);

            reducer.Apply(card);

            Assert.AreEqual(0, card.Value);
        }

        private class DummyCard : ICard
        {
            private int _value;

            public DummyCard(int initialValue)
            {
                _value = initialValue;
            }

            public string Name => "Dummy";

            public int? Value
            {
                get => _value;
                set => _value = value ?? 0;
            }

            public List<ISticker> Stickers { get; } = new();

            public ICard Clone()
            {
                return new DummyCard(_value);
            }

            public void ModifyValue(int delta)
            {
                _value += delta;
            }
        }
    }
}