using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.ModLoading;
using TMPro;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class CardView : MonoBehaviour
    {
        public TextMeshPro titleText;
        public TextMeshPro descriptionText;
        public TextMeshPro treatmentCostText;
        public Material cardMaterial;

        [Tooltip("Where sticker visuals will be parented on this card")]
        public Transform stickerHolder;

        private DeckManager _deckManager;
        private ICard _originalCard;

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
        }

        public ICard GetCard()
        {
            return _originalCard;
        }

        public void Setup(ICard card)
        {
            var title = card.Name ?? string.Empty;
            if (card is RuntimeCard)
                title += " [MOD]";
            titleText.text = title;
            cardMaterial = card.Material ? card.Material : DefaultMaterials.White;
            var cardRenderer = GetComponentInChildren<Renderer>(true);
            if (cardRenderer)
                cardRenderer.material = cardMaterial;
            else
                Debug.LogWarning("CardView: No Renderer found on card prefab; skipping material assignment.");
            descriptionText.text = card.Description ?? string.Empty;
            if (card.Value != null) treatmentCostText.text = card.Value.ToString();
            _originalCard = card;

            RestoreStickerVisuals();
        }

        private void RestoreStickerVisuals()
        {
            if (_originalCard?.Stickers == null || !stickerHolder) return;


            foreach (Transform child in stickerHolder) Destroy(child.gameObject);

            foreach (var click3D in from sticker in _originalCard.Stickers
                     where sticker?.Prefab != null
                     select Instantiate(sticker.Prefab, stickerHolder, false)
                     into stickerInstance
                     where stickerInstance != null
                     select stickerInstance.GetComponent<Click3D>()
                     into click3D
                     where click3D != null
                     select click3D) click3D.enabled = false;
        }

        public void CardClicked(Click3D clickedCard)
        {
            if (CardGameMaster.Instance?.isInspecting == true) return;
            var dm = _deckManager ?? CardGameMaster.Instance?.deckManager;
            var drag = dm != null ? dm.SelectedSticker : null;
            if (dm != null && drag != null)
            {
                dm.TryDropStickerOn(_originalCard, drag);
                if (!stickerHolder || !drag.definition?.Prefab) return;
                var stickerInstance = Instantiate(drag.definition.Prefab, stickerHolder, false);
                if (stickerInstance == null) return;
                var click3D = stickerInstance.GetComponent<Click3D>();
                if (click3D != null) click3D.enabled = false;
                if (treatmentCostText != null)
                    treatmentCostText.text = "$ " + (_originalCard.Value ?? 0);

                return;
            }

            if (_deckManager == null) return;

            if (_deckManager.selectedACardClick3D == clickedCard)
            {
                clickedCard.selected = false;
                StartCoroutine(clickedCard.AnimateCardBack());
                _deckManager.ClearSelectedCard();
                return;
            }

            if (_deckManager.selectedACardClick3D != null)
            {
                var selCard = _deckManager.selectedACardClick3D;
                selCard.selected = false;
                StartCoroutine(selCard.AnimateCardBack());
            }

            _deckManager.SetSelectedCard(clickedCard, _originalCard);
            _originalCard?.Selected();
            clickedCard.selected = true;
            CardGameMaster.Instance!.playerHandAudioSource.PlayOneShot(
                CardGameMaster.Instance.soundSystem.selectCard);
        }
    }
}
