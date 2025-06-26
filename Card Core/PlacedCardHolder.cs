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

        private static CardGameMaster Cgm => CardGameMaster.Instance;

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
            _scoreManager = CardGameMaster.Instance.scoreManager;
        }

        /// <summary>
        ///     Takes the currently selected card from the deck and places it into the cardholder.
        /// </summary>
        /// <remarks>
        ///     If a card is already being held by the cardholder, it is returned before the new card is taken.
        ///     This method ensures the proper management of card interactions, such as cloning the selected card,
        ///     setting up its view, and positioning it within the holder. Additionally, it hides the original card
        ///     and updates relevant scoring data, including treatment costs.
        /// </remarks>
        public void TakeSelectedCard()
        {
            if (HoldingCard) GiveBackCard();
            if (_deckManager.selectedACardClick3D is null || _deckManager.SelectedACard is null) return;

            var selectedCard = _deckManager.selectedACardClick3D;

            selectedCard.DisableClick3D();
            Cgm.playerHandAudioSource.PlayOneShot(Cgm.soundSystem.placeCard);

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
        ///     Returns the currently held card from the cardholder to its appropriate location.
        /// </summary>
        /// <remarks>
        ///     This method ensures proper restoration of the held card by either returning it to the player's hand or
        ///     moving it back to the retained cardholder, if applicable. If the card cannot be returned to either,
        ///     its clone is destroyed. The method also updates the game state by recalculating treatment costs, clearing
        ///     this holder's state, and enabling the visibility of the necessary UI elements. Additionally, it plays an
        ///     audio cue to signify the card's removal from the holder.
        /// </remarks>
        private void GiveBackCard()
        {
            if (!HoldingCard) return;

            var returnedToHand = false;
            var returnedToRetained = false;

            // Check for returning to the original hand
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
                        // The Card was from the retained slot but returned to hand or lost — cleanup retained slot
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
                    _scoreManager.treatmentCost -= PlacedCard.Value.Value;
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