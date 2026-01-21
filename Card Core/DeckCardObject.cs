using _project.Scripts.Classes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _project.Scripts.Card_Core
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(DraggableCard))]
    public class DeckCardObject : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image cardImage;

        public IShopItem ShopItem { get; private set; }

        public void Setup(IShopItem shopItem)
        {
            ShopItem = shopItem;

            var image = cardImage != null ? cardImage : GetComponent<Image>();
            if (image is null) return;

            var cardMaterial = ShopItem?.Card?.Material;
            if (cardMaterial is null)
            {
                image.material = null;
                image.sprite = null;
                return;
            }

            if (cardMaterial.mainTexture is not Texture2D cardTexture)
            {
                image.material = null;
                image.sprite = null;
                Debug.LogWarning("DeckCardObject: Card material has no texture; sprite cleared.");
                return;
            }

            image.material = null;
            image.sprite = Sprite.Create(
                cardTexture,
                new Rect(0f, 0f, cardTexture.width, cardTexture.height),
                new Vector2(0.5f, 0.5f));
        }
    }
}