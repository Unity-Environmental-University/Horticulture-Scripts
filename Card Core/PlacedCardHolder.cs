using System.Collections;
using System.Reflection;
using _project.Scripts.Classes;
using UnityEngine;
using UnityEngine.InputSystem;

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
        
        private int _lastPlacementFrame = -1;
        private float _lastPlacementTime = -1f;
        private float _lastClickTime = -1f;

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
        ///     setting up its view, and positioning it within the holder. The CardView component is preserved
        ///     on the clone to enable treatment application during the card game flow. Additionally, it hides 
        ///     the original card and updates relevant scoring data, including treatment costs.
        /// </remarks>
        /// <summary>
        /// Called when the placed card is clicked. Handles pickup and swapping logic.
        /// </summary>
        public void OnPlacedCardClicked()
        {
            Debug.Log($"[CARD BOUNCE DEBUG] OnPlacedCardClicked called. Frame: {Time.frameCount}, LastPlacement: {_lastPlacementFrame}, Time: {Time.time:F2}, LastPlacementTime: {_lastPlacementTime:F2}, LastClickTime: {_lastClickTime:F2}, HoldingCard: {HoldingCard}, SelectedACard: {(_deckManager.selectedACard?.GetType().Name ?? "null")}, PlacedCard: {(PlacedCard?.GetType().Name ?? "null")}");
            
            // Prevent duplicate clicks within the same frame (Input System double-processing)
            if (Time.time - _lastClickTime < 0.1f)
            {
                Debug.Log($"[CARD BOUNCE DEBUG] Ignoring duplicate click ({Time.time - _lastClickTime:F3}s since last)");
                return;
            }
            _lastClickTime = Time.time;
            
            // Prevent clicks in the same frame as placement to avoid input system double-processing
            if (Time.frameCount == _lastPlacementFrame)
            {
                Debug.Log("[CARD BOUNCE DEBUG] Ignoring click - same frame as placement");
                return;
            }
            
            // Prevent clicks within 0.5 seconds of placement to avoid accidental immediate pickup
            if (Time.time - _lastPlacementTime < 0.5f)
            {
                Debug.Log($"[CARD BOUNCE DEBUG] Ignoring click - too soon after placement ({Time.time - _lastPlacementTime:F2}s)");
                return;
            }
            
            // If the holder is empty, treat this as a normal placement
            if (!HoldingCard)
            {
                Debug.Log("[CARD BOUNCE DEBUG] Holder empty, calling TakeSelectedCard");
                TakeSelectedCard();
                return;
            }
            
            // If no card is selected in the hand, pick up this placed card
            if (_deckManager.selectedACard == null)
            {
                Debug.Log("[CARD BOUNCE DEBUG] No card selected in hand, picking up placed card");
                PickUpPlacedCard();
                return;
            }
            
            // If the selected card is the same as the placed card, pick it up (user clicked their own placed card)
            if (_deckManager.selectedACard == PlacedCard)
            {
                Debug.Log("[CARD BOUNCE DEBUG] Selected card same as placed card, picking up");
                PickUpPlacedCard();
                return;
            }
            
            // If a different card is selected in the hand, swap it with this placed card
            Debug.Log("[CARD BOUNCE DEBUG] Swapping with selected card");
            SwapWithSelectedCard();
        }
        
        /// <summary>
        /// Picks up the placed card and puts it back in the hand
        /// </summary>
        private void PickUpPlacedCard()
        {
            Debug.Log($"[CARD BOUNCE DEBUG] PickUpPlacedCard called. HoldingCard: {HoldingCard}");
            if (!HoldingCard) return;
            
            // Play pickup sound
            Cgm.playerHandAudioSource.PlayOneShot(Cgm.soundSystem.selectCard);
            
            // Clear any existing selection first to prevent multiple selected cards
            ClearAllSelections();
            
            // Update cost before clearing (subtract the cost since we're removing the card)
            if (PlacedCard?.Value != null)
            {
                var retained = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                var isFromRetained = retained && retained.HeldCard == PlacedCard;
                
                if (!isFromRetained)
                    _scoreManager.treatmentCost -= PlacedCard.Value.Value;
            }
            
            // Find the original card in the hand and re-enable it
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                if (cardView.GetCard() != PlacedCard) continue;
                
                // Re-enable the card in the hand
                foreach (var rend in cardView.GetComponentsInChildren<Renderer>(true))
                    rend.enabled = true;
                    
                var click3D = cardView.GetComponent<Click3D>();
                if (click3D != null)
                {
                    click3D.enabled = true;
                    click3D.isEnabled = true;
                    click3D.selected = false; // Don't select it when picked up
                }
                
                break;
            }
            
            // Clear this holder (destroys the clone and resets the state)
            ClearHolder();
            
            // Recalculate treatment cost
            _scoreManager.CalculateTreatmentCost();
        }
        
        /// <summary>
        /// Swaps the currently selected card with the placed card
        /// </summary>
        private void SwapWithSelectedCard()
        {
            if (!HoldingCard || _deckManager.selectedACard == null) return;
            
            // Play swap sound
            Cgm.playerHandAudioSource.PlayOneShot(Cgm.soundSystem.placeCard);
            
            // Store what we're working with
            var selectedCard = _deckManager.selectedACard;
            var selectedClick3D = _deckManager.selectedACardClick3D;
            var currentPlacedCard = PlacedCard;
            var currentCardClone = placedCardClick3D.gameObject;
            
            // Update costs - remove old placed card cost, will add new one in TakeSelectedCard
            if (currentPlacedCard?.Value != null)
            {
                var retained = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                var isFromRetained = retained && retained.HeldCard == currentPlacedCard;
                if (!isFromRetained)
                    _scoreManager.treatmentCost -= currentPlacedCard.Value.Value;
            }
            
            // IMPORTANT: Clear the holder state BEFORE calling TakeSelectedCard
            // This ensures HoldingCard returns false and TakeSelectedCard won't call GiveBackCard
            PlacedCard = null;
            placedCardClick3D = null;
            placedCardView = null;
            
            // Destroy the old-placed card visual
            if (currentCardClone != null)
                Destroy(currentCardClone);
            
            // Now place the selected card - since HoldingCard is false, it won't call GiveBackCard
            TakeSelectedCard();
            
            // Put the old-placed card back in the hand without selecting it
            RestoreCardToHandWithoutSelection(currentPlacedCard);
            
            // Final safety check - ensure no cards are selected after swap
            // Clear manager state
            _deckManager.selectedACardClick3D = null;
            _deckManager.selectedACard = null;
            
            // Also clear visual selection state on all hand cards to prevent any lingering selections
            var allHandCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in allHandCards)
            {
                var click3D = cardView.GetComponent<Click3D>();
                if (click3D != null && click3D.selected)
                {
                    click3D.selected = false;
                    click3D.StartCoroutine(click3D.AnimateCardBack());
                }
            }
            
            // Recalculate treatment cost
            _scoreManager.CalculateTreatmentCost();
        }
        
        /// <summary>
        /// Clears all card selections in the hand
        /// </summary>
        private void ClearAllSelections()
        {
            // Clear deck manager selections
            _deckManager.selectedACardClick3D = null;
            _deckManager.selectedACard = null;
            
            // Find all hand cards and deselect them
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                var click3D = cardView.GetComponent<Click3D>();
                if (click3D != null && click3D.selected)
                {
                    click3D.selected = false;
                    click3D.StartCoroutine(click3D.AnimateCardBack());
                }
            }
        }
        
        /// <summary>
        /// Restores a card to the hand as the selected card
        /// </summary>
        private void RestoreCardToHand(ICard card)
        {
            // First, deselect any currently selected card
            if (_deckManager.selectedACardClick3D != null)
            {
                _deckManager.selectedACardClick3D.selected = false;
                _deckManager.selectedACardClick3D.StartCoroutine(_deckManager.selectedACardClick3D.AnimateCardBack());
            }
            
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                if (cardView.GetCard() != card) continue;
                
                // Re-enable the card in the hand
                foreach (var rend in cardView.GetComponentsInChildren<Renderer>(true))
                    rend.enabled = true;
                    
                var click3D = cardView.GetComponent<Click3D>();
                if (click3D != null)
                {
                    click3D.enabled = true;
                    click3D.isEnabled = true;
                    click3D.selected = true;
                }
                
                // Set it as selected
                _deckManager.selectedACardClick3D = click3D;
                _deckManager.selectedACard = card;
                
                break;
            }
        }
        
        /// <summary>
        /// Restores a card to the hand without selecting it
        /// </summary>
        private void RestoreCardToHandWithoutSelection(ICard card)
        {
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                if (cardView.GetCard() != card) continue;
                
                // Re-enable the card in the hand
                foreach (var rend in cardView.GetComponentsInChildren<Renderer>(true))
                    rend.enabled = true;
                    
                var click3D = cardView.GetComponent<Click3D>();
                if (click3D != null)
                {
                    click3D.enabled = true;
                    click3D.isEnabled = true;
                    click3D.selected = false; // Don't select it
                    // Make sure it's not raised/animated
                    click3D.StartCoroutine(click3D.AnimateCardBack());
                }
                
                break;
            }
        }
        
        /// <summary>
        /// Clears the holder without returning the card to hand
        /// </summary>
        private void ClearHolder()
        {
            if (placedCardClick3D != null)
                Destroy(placedCardClick3D.gameObject);
                
            PlacedCard = null;
            placedCardClick3D = null;
            placedCardView = null;
            
            // Show the button/holder again
            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer) buttonRenderer.enabled = true;
        }

        public void TakeSelectedCard()
        {
            Debug.Log($"[CARD BOUNCE DEBUG] TakeSelectedCard called. HoldingCard: {HoldingCard}, SelectedACard: {(_deckManager.selectedACard?.GetType().Name ?? "null")}");
            
            if (HoldingCard) 
            {
                Debug.Log("[CARD BOUNCE DEBUG] Already holding card, giving it back first");
                GiveBackCard();
            }
            if (_deckManager.selectedACardClick3D is null || _deckManager.selectedACard is null) 
            {
                Debug.Log("[CARD BOUNCE DEBUG] No selected card to take, returning early");
                return;
            }

            var selectedCard = _deckManager.selectedACardClick3D;
            Debug.Log($"[CARD BOUNCE DEBUG] Taking selected card: {_deckManager.selectedACard.GetType().Name}");

            // Properly disable the original card's Click3D component to prevent duplicate clicks
            selectedCard.DisableClick3D();
            selectedCard.enabled = false;
            Cgm.playerHandAudioSource.PlayOneShot(Cgm.soundSystem.placeCard);

            // Clone the card and all its components
            var cardClone = Instantiate(selectedCard.gameObject, transform);

            // Set up the CloneCardView
            var cardViewClone = cardClone.GetComponent<CardView>();
            if (cardViewClone)
                cardViewClone.Setup(_deckManager.selectedACard);

            // Set parent without preserving world values
            //originalCardTransform = selectedCard.transform;
            cardClone.transform.SetParent(transform, false);

            // Snap to the transform exactly (position, rotation, scale)
            cardClone.transform.localPosition = Vector3.zero;
            cardClone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Lying flat
            cardClone.transform.localScale = Vector3.one; // or the original prefab scale, if different
            cardClone.transform.localScale = Vector3.one * 0.9f;

            // Hold the Card Data
            PlacedCard = _deckManager.selectedACard;
            placedCardClick3D = cardClone.GetComponent<Click3D>();
            placedCardView = cardClone.GetComponent<CardView>();
            
            // Set up the click handler for the placed card
            if (placedCardClick3D != null)
            {
                placedCardClick3D.onClick3D.RemoveAllListeners();
                placedCardClick3D.onClick3D.AddListener(OnPlacedCardClicked);
                // Disable the component initially to prevent immediate clicks
                placedCardClick3D.enabled = false;
                Debug.Log($"[CARD BOUNCE DEBUG] Card placed, Click3D component disabled temporarily");
                StartCoroutine(ReenablePlacedCardClickWithInputActionFix());
            }
            
            _lastPlacementFrame = Time.frameCount;
            _lastPlacementTime = Time.time;

            _deckManager.selectedACardClick3D = null;
            _deckManager.selectedACard = null;

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

        private IEnumerator ReenablePlacedCardClickWithInputActionFix()
        {
            // Wait a few frames to ensure placement click is processed
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
            
            if (placedCardClick3D != null) 
            {
                // Re-enable the component but destroy the InputAction to prevent double-processing
                placedCardClick3D.enabled = true;
                placedCardClick3D.isEnabled = true;
                
                // CORE FIX: Disable the InputAction on the clone after it's been created
                var reflection = placedCardClick3D.GetType();
                var inputActionField = reflection.GetField("_mouseClickAction", BindingFlags.NonPublic | BindingFlags.Instance);
                if (inputActionField != null)
                {
                    var inputAction = inputActionField.GetValue(placedCardClick3D) as InputAction;
                    if (inputAction != null)
                    {
                        inputAction.Disable();
                        inputAction.Dispose();
                        inputActionField.SetValue(placedCardClick3D, null);
                        Debug.Log($"[CARD BOUNCE DEBUG] Clone's InputAction disabled via reflection, component re-enabled for mobile");
                    }
                }
            }
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
                    click3D.enabled = true;
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
