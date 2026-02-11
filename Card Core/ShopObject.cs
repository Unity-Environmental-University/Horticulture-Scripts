using _project.Scripts.Classes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _project.Scripts.Card_Core
{
    public class ShopObject : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Image objectImage;
        [SerializeField] private Button buyButton;

        public IShopItem ShopItem { get; private set; }

        public void Setup(IShopItem shopItem)
        {
            ShopItem = shopItem;

            titleText.text = ShopItem.DisplayName;
            costText.text = "-$" + ShopItem.Cost;

            var image = objectImage ? objectImage : GetComponent<Image>();
            if (image is null) return;

            var cardMaterial = ShopItem?.DisplayMaterial;
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
                Debug.LogWarning("ShopObject: Card material has no texture; sprite cleared.");
                return;
            }

            image.material = null;
            image.sprite = Sprite.Create(
                cardTexture,
                new Rect(0f, 0f, cardTexture.width, cardTexture.height),
                new Vector2(0.5f, 0.5f));

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => ShopItem.Purchase());
        }
    }
}