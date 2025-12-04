using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Stickers;
using NUnit.Framework;
using UnityEngine;

namespace _project.Scripts.PlayModeTest
{
    public class StickerPersistenceTest
    {
        [Test]
        public void ActionCard_Clone_PreservesStickers()
        {
            // Arrange
            ICard originalCard = new HorticulturalOilBasic();
            var sticker = ScriptableObject.CreateInstance<StickerDefinition>();
            sticker.stickerName = "TestSticker";

            // Act
            originalCard.ApplySticker(sticker);
            var clonedCard = originalCard.Clone();

            // Assert
            Assert.AreEqual(1, originalCard.Stickers.Count, "Original card should have 1 sticker");
            Assert.AreEqual("TestSticker", originalCard.Stickers.First().Name,
                "Original card should have the test sticker");
            Assert.AreEqual(1, clonedCard.Stickers.Count, "Cloned card should preserve stickers");
            Assert.AreEqual("TestSticker", clonedCard.Stickers.First().Name,
                "Cloned card should have the same sticker name");
            Assert.AreEqual(1, originalCard.Stickers.Count, "Original card should still have 1 sticker");
        }

        [Test]
        public void AfflictionCard_Clone_PreservesStickers()
        {
            // Arrange
            ICard originalCard = new ThripsCard();
            var sticker = ScriptableObject.CreateInstance<StickerDefinition>();
            sticker.stickerName = "TestAfflictionSticker";

            // Act
            originalCard.ApplySticker(sticker);
            var clonedCard = originalCard.Clone();

            // Assert
            Assert.AreEqual(1, originalCard.Stickers.Count, "Original card should have 1 sticker");
            Assert.AreEqual(1, clonedCard.Stickers.Count, "Cloned affliction card should preserve stickers");
            Assert.AreEqual("TestAfflictionSticker", clonedCard.Stickers.First().Name,
                "Cloned card should have the same sticker name");
        }

        [Test]
        public void PlantCard_Clone_PreservesStickers()
        {
            // Arrange
            ICard originalCard = new ColeusCard();
            var sticker = ScriptableObject.CreateInstance<StickerDefinition>();
            sticker.stickerName = "TestPlantSticker";

            // Act
            originalCard.ApplySticker(sticker);
            var clonedCard = originalCard.Clone();

            // Assert
            Assert.AreEqual(1, originalCard.Stickers.Count, "Original plant card should have 1 sticker");
            Assert.AreEqual(1, clonedCard.Stickers.Count, "Cloned plant card should preserve stickers");
            Assert.AreEqual("TestPlantSticker", clonedCard.Stickers.First().Name,
                "Cloned card should have the same sticker name");
        }
    }
}