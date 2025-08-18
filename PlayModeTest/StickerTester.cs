using System.Collections.Generic;
using _project.Scripts.Classes;
using _project.Scripts.Stickers;
using NUnit.Framework;
using UnityEngine;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Tests custom sticker functionality, e.g., ValueReducerSticker and base StickerDefinition behavior.
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

        [Test]
        public void ValueReducerSticker_MultipleApplications_TransitionsAcrossZero()
        {
            var reducer = ScriptableObject.CreateInstance<ValueReducerSticker>();
            reducer.reductionAmount = 2;
            var card = new DummyCard(3); // 3 -> 1 -> -1

            reducer.Apply(card);
            Assert.AreEqual(1, card.Value);

            reducer.Apply(card);
            Assert.AreEqual(-1, card.Value);
        }

        [Test]
        public void ValueReducerSticker_MultipleApplications_NegativeToPositive()
        {
            var reducer = ScriptableObject.CreateInstance<ValueReducerSticker>();
            reducer.reductionAmount = 2;
            var card = new DummyCard(-3); // -3 -> -1 -> 1

            reducer.Apply(card);
            Assert.AreEqual(-1, card.Value);

            reducer.Apply(card);
            Assert.AreEqual(1, card.Value);
        }

        [Test]
        public void StickerDefinition_Apply_AddsStickerToCard()
        {
            var sticker = ScriptableObject.CreateInstance<StickerDefinition>();
            sticker.stickerName = "TestSticker";
            var card = new DummyCard(5);

            Assert.AreEqual(0, card.Stickers.Count);
            sticker.Apply(card);
            Assert.AreEqual(1, card.Stickers.Count);
            Assert.AreSame(sticker, card.Stickers[0]);
        }

        [Test]
        public void StickerDefinition_Clone_ReturnsSameInstance()
        {
            var sticker = ScriptableObject.CreateInstance<StickerDefinition>();
            var clone = sticker.Clone();
            Assert.AreSame(sticker, clone);
        }

        [Test]
        public void StickerDefinition_Value_GetSet_Works()
        {
            var sticker = ScriptableObject.CreateInstance<StickerDefinition>();
            sticker.Value = 7;
            Assert.AreEqual(7, sticker.Value);
            sticker.Value = null;
            Assert.AreEqual(0, sticker.Value); // null sets to 0 by implementation
        }

        [Test]
        public void CopyCardSticker_Apply_ClonesCardAndInvokesHandlerOnce()
        {
            // Arrange
            var sticker = ScriptableObject.CreateInstance<TestCopyCardSticker>();
            var original = new DummyCard(12);

            // Precondition: no stickers yet
            Assert.AreEqual(0, original.Stickers.Count);

            // Act
            sticker.Apply(original);

            // Assert
            // 1) Base Apply should add the sticker to the card
            Assert.AreEqual(1, original.Stickers.Count);
            Assert.AreSame(sticker, original.Stickers[0]);

            // 2) Our test seam should have been invoked exactly once with a clone
            Assert.AreEqual(1, sticker.HandleCalls);
            Assert.NotNull(sticker.LastCloned);
            Assert.AreNotSame(original, sticker.LastCloned);

            // 3) The clone should preserve value/state (DummyCard copies value)
            Assert.AreEqual(original.Value, sticker.LastCloned.Value);
        }

        private class TestCopyCardSticker : CopyCardSticker
        {
            public int HandleCalls { get; private set; }
            public ICard LastCloned { get; private set; }

            protected override void HandleClonedCard(ICard cloned)
            {
                HandleCalls++;
                LastCloned = cloned;
                // Do NOT call base (avoids needing CardGameMaster/DeckManager in tests)
            }
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