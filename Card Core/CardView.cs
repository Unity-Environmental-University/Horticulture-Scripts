using System.Linq;
using _project.Scripts.Classes;
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
            titleText.text = card.Name;
            cardMaterial = card.Material;
            var cardRenderer = GetComponentInChildren<Renderer>(true);
            if (cardRenderer)
                cardRenderer.material = cardMaterial;
            else
                Debug.LogWarning("CardView: No Renderer found on card prefab; skipping material assignment.");
            descriptionText.text = card.Description;
            if (card.Value != null) treatmentCostText.text = "$ " + card.Value;
            _originalCard = card;

            RestoreStickerVisuals();
        }

        private void RestoreStickerVisuals()
        {
            if (_originalCard?.Stickers == null || !stickerHolder) return;

            foreach (Transform child in stickerHolder) Destroy(child.gameObject);
            foreach (var click3D in from sticker in _originalCard.Stickers
                     where sticker?.Prefab
                     select Instantiate(sticker?.Prefab, stickerHolder, false)
                     into stickerInstance
                     select stickerInstance.GetComponent<Click3D>()
                     into click3D
                     where click3D
                     select click3D) click3D.enabled = false;
        }

        public void CardClicked(Click3D clickedCard)
        {
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

            if (_deckManager == null)
            {
                Debug.LogWarning("CardView: _deckManager is null, cannot process card click");
                return;
            }

            if (_deckManager.selectedACardClick3D == clickedCard)
            {
                clickedCard.selected = false;
                StartCoroutine(clickedCard.AnimateCardBack());
                _deckManager.selectedACardClick3D = null;
                _deckManager.selectedACard = null;
                return;
            }

            if (_deckManager.selectedACardClick3D != null)
            {
                var selCard = _deckManager.selectedACardClick3D;
                selCard.selected = false;
                StartCoroutine(selCard.AnimateCardBack());
            }

            _deckManager.selectedACardClick3D = clickedCard;
            _deckManager.selectedACard = _originalCard;
            _originalCard?.Selected();
            clickedCard.selected = true;
            CardGameMaster.Instance!.playerHandAudioSource.PlayOneShot(
                CardGameMaster.Instance.soundSystem.selectCard);
        }
    }
}
