using _project.Scripts.Classes;
using TMPro;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class CardView : MonoBehaviour
    {
        private DeckManager _deckManager;
        private ICard _originalCard;
        
        public TextMeshPro titleText;
        public TextMeshPro descriptionText;
        public TextMeshPro treatmentCostText;
        public Material cardMaterial;
        [Tooltip("Where sticker visuals will be parented on this card")]
        public Transform stickerHolder;

        private void Start() => _deckManager = CardGameMaster.Instance.deckManager;
        
        public ICard GetCard() => _originalCard;
        
        public void Setup(ICard card)
        {
            titleText.text = card.Name;
            cardMaterial = card.Material;
            gameObject.GetComponent<Renderer>().material = cardMaterial;
            descriptionText.text = card.Description;
            if (card.Value != null) treatmentCostText.text = "$ " + card.Value;
            _originalCard = card;
        }

        public void CardClicked(Click3D clickedCard)
        {
            // if a sticker is being dragged, drop it here instead of selecting the card
            var dm = CardGameMaster.Instance.deckManager;
            var drag = dm.SelectedSticker;
            if (drag != null)
            {
                dm.TryDropStickerOn(_originalCard, drag);
                // visually attach the sticker prefab onto this card
                if (stickerHolder && drag.definition?.Prefab)
                {
                    Instantiate(drag.definition.Prefab, stickerHolder, false);
                }
                return;
            }
            // if the clicked card is already selected, unselect it
            if (_deckManager.selectedACardClick3D == clickedCard)
            {
                clickedCard.selected = false;
                StartCoroutine(clickedCard.AnimateCardBack());
                _deckManager.selectedACardClick3D = null;
                _deckManager.SelectedACard = null;
                return;
            }

            // switch to a new card on click
            if (_deckManager.selectedACardClick3D != null)
            {
                var selCard = _deckManager.selectedACardClick3D;
                selCard.selected = false;
                StartCoroutine(selCard.AnimateCardBack());
            }

            // otherwise, select the clicked card
            _deckManager.selectedACardClick3D = clickedCard;
            _deckManager.SelectedACard = _originalCard;
            _originalCard.Selected();
            clickedCard.selected = true;
            CardGameMaster.Instance.playerHandAudioSource.PlayOneShot(
                CardGameMaster.Instance.soundSystem.selectCard);
        }
    }
}
