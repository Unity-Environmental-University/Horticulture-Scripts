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

        private void Start() => _deckManager = CardGameMaster.Instance.deckManager;
        
        public ICard GetCard() => _originalCard;
        
        public void Setup(ICard card)
        {
            titleText.text = card.Name;
            cardMaterial = card.Material;
            this.gameObject.GetComponent<Renderer>().material = cardMaterial;
            descriptionText.text = card.Description;
            if (card.Value != null) treatmentCostText.text = "$ " + card.Value;
            _originalCard = card;
        }

        public void CardClicked(Click3D clickedCard)
        {
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
        }
    }
}