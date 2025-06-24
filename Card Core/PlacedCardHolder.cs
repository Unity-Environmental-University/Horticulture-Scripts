using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class PlacedCardHolder : MonoBehaviour
    {
        public Click3D placedCardClick3D;
        public CardView placedCardView;
        private DeckManager _deckManager;
        private ScoreManager _scoreManager;
        public ICard PlacedCard;
        public bool HoldingCard => placedCardClick3D;

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
            _scoreManager = CardGameMaster.Instance.scoreManager;
        }

        /// <summary>
        ///     Handles the selection of a card and attaches it to the cardholder, effectively "placing" the card.
        ///     If a card is already placed, it will be returned to its original position before placing the selected one.
        /// </summary>
        /// <remarks>
        ///     This method:
        ///     1. Checks if a card is currently being held:
        ///     - If yes, the existing card is returned to its original position.
        ///     2. Verify if a card is selected on the deck. If no card is selected, the method exits without action.
        ///     3. Clones the selected card and its associated components, then places it in the cardholder:
        ///     - The cloned card is configured to replace the selected card.
        ///     - Its transform properties (position, rotation, scale) are updated for proper appearance.
        ///     4. Updates internal references to track the new "placed" card.
        ///     5. Hides the associated 3D click capability and visual appearance of the original card in the deck.
        /// </remarks>
        public void TakeSelectedCard()
        {
            if (HoldingCard) GiveBackCard();
            //if (HoldingCard) return;
            if (_deckManager.selectedACardClick3D is null || _deckManager.SelectedACard is null) return;

            var selectedCard = _deckManager.selectedACardClick3D;

            selectedCard.DisableClick3D();

            // Clone the card and all its components
            var cardClone = Instantiate(selectedCard.gameObject, transform);

            // Set up the CloneCardView
            var cardViewClone = cardClone.GetComponent<CardView>();
            if (cardViewClone)
                cardViewClone.Setup(_deckManager.SelectedACard);

            // Set parent without preserving world values
            //originalCardTransform = selectedCard.transform;
            cardClone.transform.SetParent(transform, false);

            // Snap to the transform exactly (position, rotation, scale)
            cardClone.transform.localPosition = Vector3.zero;
            cardClone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Lying flat
            cardClone.transform.localScale = Vector3.one; // or the original prefab scale, if different
            cardClone.transform.localScale = Vector3.one * 0.9f;

            // Hold the Card Data
            PlacedCard = _deckManager.SelectedACard;
            placedCardClick3D = cardClone.GetComponent<Click3D>();
            placedCardView = cardClone.GetComponent<CardView>();

            _deckManager.selectedACardClick3D = null;
            _deckManager.SelectedACard = null;

            // Hide the CardButton and the card in Hand
            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer)
                buttonRenderer.enabled = false;
            var cardRenderers = selectedCard.GetComponentsInChildren<Renderer>();
            if (cardRenderers == null) return;
            foreach (var renderer1 in cardRenderers) renderer1.enabled = false;

            if (PlacedCard?.Value != null)
            {
                var retained = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                var isFromRetained = retained && retained.HeldCard == PlacedCard;

                if (!isFromRetained)
                    _scoreManager.treatmentCost += PlacedCard.Value.Value;
            }
            _scoreManager.CalculateTreatmentCost();
        }

        /// <summary>
        /// Handles the process of returning a placed card to its original position or the retained cardholder.
        /// If the card is currently held, it will be returned either to the player's hand or to the retained cardholder,
        /// depending on where the original card was located. If neither location is found, the cloned card will be destroyed.
        /// </summary>
        /// <remarks>
        /// This method:
        /// 1. Checks if a card is currently being held. If not, it exits without action.
        /// 2. Search for the original card in the player's hand:
        /// - If found, re-enables the card's visual components, and associated click capabilities.
        /// - Marks the card as returned to the hand.
        /// 3. If the card was not found in the hand, it searches for an available slot in the retained cardholder.
        /// 4. If the card is neither returned to the hand nor the retained holder, it destroys the cloned card.
        /// 5. Adjust the treatment cost based on the value of the returned card.
        /// 6. Calculate the updated treatment cost.
        /// 7. Clear the state of this holder, nullifying references to the placed card and its components.
        /// 8. Re-enables the visual components of the button and parent renderer associated with this holder.
        /// </remarks>
        private void GiveBackCard()
        {
            if (!HoldingCard) return;

            var returnedToHand = false;
            var returnedToRetained = false;

            // Check for returning to original hand
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                if (cardView.GetCard() != PlacedCard) continue;
                // Found the original card in the hand, re-enable it
                foreach (var renderer1 in cardView.GetComponentsInChildren<Renderer>(true))
                    renderer1.enabled = true;

                var click3D = cardView.GetComponent<Click3D>();
                if (click3D)
                {
                    click3D.isEnabled = true;
                    click3D.selected = false;
                    click3D.RefreshState();
                    click3D.StartCoroutine(click3D.AnimateCardBack());
                }

                returnedToHand = true;
                break;
            }

            // Check for returning to RetainedCardHolder
            var retainedSlot = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
            if (retainedSlot != null)
                if (retainedSlot.HeldCard == PlacedCard)
                {
                    if (!returnedToHand)
                    {
                        // The Card belongs to the retained slot, give it back
                        retainedSlot.ReclaimCard(placedCardClick3D.gameObject);
                        returnedToRetained = true;
                    }
                    else
                    {
                        // Card was from retained slot but returned to hand or lost — cleanup retained slot
                        retainedSlot.ClearHeldCard();
                    }
                }

            // If we didn't return the clone to retain, destroy it
            if (!returnedToRetained && placedCardClick3D) Destroy(placedCardClick3D.gameObject);

            // Update cost
            if (PlacedCard?.Value != null)
            {
                var retainedSlot1 = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                if (!(retainedSlot1 != null && retainedSlot1.HeldCard == PlacedCard))
                {
                    _scoreManager.treatmentCost -= PlacedCard.Value.Value;
                }
            }
            _scoreManager.CalculateTreatmentCost();

            // Clear this holder's state
            placedCardView = null;
            placedCardClick3D = null;
            PlacedCard = null;

            var playerAudio = CardGameMaster.Instance.playerHandAudioSource;
            playerAudio.PlayOneShot(CardGameMaster.Instance.soundSystem.unplaceCard);

            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer) buttonRenderer.enabled = true;

            var parentRenderer = transform.GetComponent<Renderer>();
            if (parentRenderer) parentRenderer.enabled = true;
        }

        public void ToggleCardHolder(bool state)
        {
            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            var click3D = gameObject.GetComponentInChildren<Click3D>(true);
            click3D.isEnabled = state;
            if (buttonRenderer)
                buttonRenderer.enabled = state;
        }
    }
}