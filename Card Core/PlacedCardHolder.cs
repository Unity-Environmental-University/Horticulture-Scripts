using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class PlacedCardHolder : MonoBehaviour
    {
        public Click3D placedCardClick3D;
        public CardView placedCardView;
        public ICard PlacedCard;
        public bool HoldingCard => placedCardClick3D;
        
        private DeckManager _deckManager;
        private Transform originalCardTransform;

        private void Start() => _deckManager = CardGameMaster.Instance.deckManager;

        /// <summary>
        /// Assigns the currently selected action card to this cardholder, visually placing the card
        /// into the holder's position and transferring card data ownership. Ensures any previously
        /// placed card in this holder is first returned to its original state.
        /// Disables the interaction capabilities of the newly placed card and updates the corresponding card references.
        /// If there is no currently selected card in the deck manager, the operation is aborted.
        /// Updates the card rendering visuals and hides the graphical button associated with the cardholder.
        /// </summary>
        public void TakeSelectedCard()
        {
            // if (HoldingCard) GiveBackCard();
            if (_deckManager.selectedACardClick3D is null || _deckManager.SelectedACard is null) return;

            var selectedCard = _deckManager.selectedACardClick3D;

            selectedCard.DisableClick3D();

            // Set parent without preserving world values
            originalCardTransform = selectedCard.transform;
            selectedCard.transform.SetParent(transform, false);

            // Snap to the transform exactly (position, rotation, scale)
            selectedCard.transform.localPosition = Vector3.zero;
            selectedCard.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Lying flat
            selectedCard.transform.localScale = Vector3.one; // or the original prefab scale, if different

            // Hold the Card Data
            PlacedCard = _deckManager.SelectedACard;
            placedCardClick3D = selectedCard;
            placedCardView = selectedCard.GetComponent<CardView>();

            // Remove Card from hand
            _deckManager.DiscardActionCard(PlacedCard, false);

            _deckManager.selectedACardClick3D = null;
            _deckManager.SelectedACard = null;

            // Hide the CardButton
            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer != null)
                buttonRenderer.enabled = false;
        }

        private void GiveBackCard()
        {
            if (!HoldingCard) return;

            // Set the card back to the deck manager for handling
            placedCardClick3D.transform.SetParent(_deckManager.actionCardParent, false);

            // Snap to the transform exactly (position, rotation, scale)
            placedCardClick3D.transform.SetParent(originalCardTransform.parent, false);
            placedCardClick3D.transform.localPosition = originalCardTransform.localPosition;
            placedCardClick3D.transform.localRotation = originalCardTransform.localRotation;
            placedCardClick3D.transform.localScale = originalCardTransform.localScale;

            // Enable interaction with the card again
            //placedCardClick3D.isEnabled = true;

            // Add the card back to the deck manager's actionHand
            _deckManager.AddActionCard(placedCardView.GetCard());


            // Clear the current PlacedCardHolder references
            placedCardView = null;
            placedCardClick3D = null;
            PlacedCard = null;

            // Show the parent object renderer again
            var parentRenderer = transform.GetComponent<Renderer>();
            if (parentRenderer) parentRenderer.enabled = true;
        }

        public void ToggleCardHolder(bool state)
        {
            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer != null)
                buttonRenderer.enabled = state;
        }
    }
}