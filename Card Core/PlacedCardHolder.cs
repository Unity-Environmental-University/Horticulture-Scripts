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
        private float _lastClickTime = -1f;

        private int _lastPlacementFrame = -1;
        private float _lastPlacementTime = -1f;
        private ScoreManager _scoreManager;
        public ICard placedCard;
        public bool HoldingCard => placedCardClick3D;

        private static CardGameMaster Cgm => CardGameMaster.Instance;

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
            _scoreManager = CardGameMaster.Instance.scoreManager;
        }


        /// <summary>
        ///     Handles the click event for a placed card, determining whether to pick it up,
        ///     swap it with a selected card in the hand, or treat it as a normal placement.
        /// </summary>
        private void OnPlacedCardClicked()
        {
            // Help reduce wierd place/unplace bugs
            if (Time.time - _lastClickTime < 0.1f) return;
            _lastClickTime = Time.time;
            if (Time.frameCount == _lastPlacementFrame) return;
            if (Time.time - _lastPlacementTime < 0.5f) return;

            if (!HoldingCard)
            {
                TakeSelectedCard();
                return;
            }

            if (_deckManager.selectedACard == null || _deckManager.selectedACard == placedCard)
            {
                PickUpPlacedCard();
                return;
            }

            SwapWithSelectedCard();
        }

        private void PickUpPlacedCard()
        {
            if (!HoldingCard) return;

            Cgm.playerHandAudioSource.PlayOneShot(Cgm.soundSystem.unplaceCard);

            ClearAllSelections();

            if (placedCard?.Value != null)
            {
                var retained = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                var isFromRetained = retained && retained.HeldCard == placedCard;

                if (!isFromRetained)
                    _scoreManager.treatmentCost -= placedCard.Value.Value;
            }

            // Find the original card in the hand and re-enable it
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                if (cardView.GetCard() != placedCard) continue;

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

            ClearHolder();

            _scoreManager.CalculateTreatmentCost();
        }

        private void SwapWithSelectedCard()
        {
            if (!HoldingCard || _deckManager.selectedACard == null) return;
            
            Cgm.playerHandAudioSource.PlayOneShot(Cgm.soundSystem.placeCard);
            
            var currentPlacedCard = placedCard;
            var currentCardClone = placedCardClick3D.gameObject;

            // Update costs - remove old placed card cost, will add new one in TakeSelectedCard
            if (currentPlacedCard?.Value != null)
            {
                var retained = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                var isFromRetained = retained && retained.HeldCard == currentPlacedCard;
                if (!isFromRetained)
                    _scoreManager.treatmentCost -= currentPlacedCard.Value.Value;
            }
            
            placedCard = null;
            placedCardClick3D = null;
            placedCardView = null;
            
            if (currentCardClone != null)
                Destroy(currentCardClone);
            
            TakeSelectedCard();
            
            RestoreCardToHandWithoutSelection(currentPlacedCard);
            
            _deckManager.selectedACardClick3D = null;
            _deckManager.selectedACard = null;
            
            var allHandCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in allHandCards)
            {
                var click3D = cardView.GetComponent<Click3D>();
                if (click3D == null || !click3D.selected) continue;
                click3D.selected = false;
                click3D.StartCoroutine(click3D.AnimateCardBack());
            }
            
            _scoreManager.CalculateTreatmentCost();
        }

        /// <summary>
        ///     Clears all card selections in the hand
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
                if (click3D == null || !click3D.selected) continue;
                click3D.selected = false;
                click3D.StartCoroutine(click3D.AnimateCardBack());
            }
        }

        /// <summary>
        ///     Restores a card to the hand as the selected card
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
        ///     Restores a card to the hand without selecting it
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
        ///     Clears the holder without returning the card to hand
        /// </summary>
        private void ClearHolder()
        {
            if (placedCardClick3D != null)
                Destroy(placedCardClick3D.gameObject);

            placedCard = null;
            placedCardClick3D = null;
            placedCardView = null;

            // Show the button/holder again
            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer) buttonRenderer.enabled = true;
        }

        public void TakeSelectedCard()
        {
            Debug.Log(
                $"[CARD BOUNCE DEBUG] TakeSelectedCard called. HoldingCard: {HoldingCard}, SelectedACard: {_deckManager.selectedACard?.GetType().Name ?? "null"}");

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
            placedCard = _deckManager.selectedACard;
            placedCardClick3D = cardClone.GetComponent<Click3D>();
            placedCardView = cardClone.GetComponent<CardView>();

            // Set up the click handler for the placed card
            if (placedCardClick3D != null)
            {
                placedCardClick3D.onClick3D.RemoveAllListeners();
                placedCardClick3D.onClick3D.AddListener(OnPlacedCardClicked);
                // Disable the component initially to prevent immediate clicks
                placedCardClick3D.enabled = false;
                Debug.Log("[CARD BOUNCE DEBUG] Card placed, Click3D component disabled temporarily");
                StartCoroutine(ReenablePlacedCardClickWithInputActionFix());
            }

            _lastPlacementFrame = Time.frameCount;
            _lastPlacementTime = Time.time;

            // Disable CardView component on placed card to prevent it from handling clicks
            if (placedCardView != null)
            {
                placedCardView.enabled = false;
                Debug.Log("[CARD BOUNCE DEBUG] CardView component disabled on placed card");
            }

            _deckManager.selectedACardClick3D = null;
            _deckManager.selectedACard = null;

            // Hide the CardButton and the card in Hand
            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer)
                buttonRenderer.enabled = false;
            var cardRenderers = selectedCard.GetComponentsInChildren<Renderer>();
            if (cardRenderers == null) return;
            foreach (var renderer1 in cardRenderers) renderer1.enabled = false;

            if (placedCard?.Value != null)
            {
                var retained = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                var isFromRetained = retained && retained.HeldCard == placedCard;

                if (!isFromRetained)
                    _scoreManager.treatmentCost += placedCard.Value.Value;
            }

            _scoreManager.CalculateTreatmentCost();
        }

        private IEnumerator ReenablePlacedCardClickWithInputActionFix()
        {
            // Wait a few frames to ensure placement click is processed
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            if (placedCardClick3D == null) yield break;
            // Re-enable the component but destroy the InputAction to prevent double-processing
            placedCardClick3D.enabled = true;
            placedCardClick3D.isEnabled = true;

            // CORE FIX: Disable the InputAction on the clone after it's been created
            var reflection = placedCardClick3D.GetType();
            var inputActionField =
                reflection.GetField("_mouseClickAction", BindingFlags.NonPublic | BindingFlags.Instance);
            if (inputActionField == null) yield break;
            var inputAction = inputActionField.GetValue(placedCardClick3D) as InputAction;
            if (inputAction == null) yield break;
            inputAction.Disable();
            inputAction.Dispose();
            inputActionField.SetValue(placedCardClick3D, null);
            Debug.Log(
                "[CARD BOUNCE DEBUG] Clone's InputAction disabled via reflection, component re-enabled for mobile");
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
                if (cardView.GetCard() != placedCard) continue;
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
                if (retainedSlot.HeldCard == placedCard)
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
            if (placedCard?.Value != null)
            {
                var retainedSlot1 = FindFirstObjectByType<RetainedCardHolder>(FindObjectsInactive.Include);
                if (!(retainedSlot1 != null && retainedSlot1.HeldCard == placedCard))
                    _scoreManager.treatmentCost -= placedCard.Value.Value;
            }

            _scoreManager.CalculateTreatmentCost();

            // Clear this holder's state
            placedCardView = null;
            placedCardClick3D = null;
            placedCard = null;

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