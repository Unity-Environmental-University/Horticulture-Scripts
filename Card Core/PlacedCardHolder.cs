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

            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                if (cardView.GetCard() != placedCard) continue;

                foreach (var rend in cardView.GetComponentsInChildren<Renderer>(true))
                    rend.enabled = true;

                var click3D = cardView.GetComponent<Click3D>();
                if (click3D != null)
                {
                    click3D.enabled = true;
                    click3D.isEnabled = true;
                    click3D.selected = false;
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

        private void ClearAllSelections()
        {
            _deckManager.selectedACardClick3D = null;
            _deckManager.selectedACard = null;
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                var click3D = cardView.GetComponent<Click3D>();
                if (click3D == null || !click3D.selected) continue;
                click3D.selected = false;
                click3D.StartCoroutine(click3D.AnimateCardBack());
            }
        }

        private void RestoreCardToHandWithoutSelection(ICard card)
        {
            var handCards = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in handCards)
            {
                if (cardView.GetCard() != card) continue;

                foreach (var rend in cardView.GetComponentsInChildren<Renderer>(true))
                    rend.enabled = true;

                var click3D = cardView.GetComponent<Click3D>();
                if (click3D != null)
                {
                    click3D.enabled = true;
                    click3D.isEnabled = true;
                    click3D.selected = false;
                    click3D.StartCoroutine(click3D.AnimateCardBack());
                }

                break;
            }
        }

        private void ClearHolder()
        {
            if (placedCardClick3D != null)
                Destroy(placedCardClick3D.gameObject);

            placedCard = null;
            placedCardClick3D = null;
            placedCardView = null;

            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer) buttonRenderer.enabled = true;
        }

        public void TakeSelectedCard()
        {
            if (HoldingCard) GiveBackCard();

            if (_deckManager.selectedACardClick3D is null || _deckManager.selectedACard is null) return;

            var selectedCard = _deckManager.selectedACardClick3D;

            // Properly disable the original card's Click3D component to prevent duplicate clicks
            selectedCard.DisableClick3D();
            selectedCard.enabled = false;
            Cgm.playerHandAudioSource.PlayOneShot(Cgm.soundSystem.placeCard);

            var cardClone = Instantiate(selectedCard.gameObject, transform);

            var cardViewClone = cardClone.GetComponent<CardView>();
            if (cardViewClone)
                cardViewClone.Setup(_deckManager.selectedACard);

            cardClone.transform.SetParent(transform, false);
            cardClone.transform.localPosition = Vector3.zero;
            cardClone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            cardClone.transform.localScale = Vector3.one * 0.9f;
            placedCard = _deckManager.selectedACard;
            placedCardClick3D = cardClone.GetComponent<Click3D>();
            placedCardView = cardClone.GetComponent<CardView>();

            if (placedCardClick3D != null)
            {
                placedCardClick3D.onClick3D.RemoveAllListeners();
                placedCardClick3D.onClick3D.AddListener(OnPlacedCardClicked);
                placedCardClick3D.enabled = false;
                StartCoroutine(ReenablePlacedCardClickWithInputActionFix());
            }

            _lastPlacementFrame = Time.frameCount;
            _lastPlacementTime = Time.time;

            if (placedCardView != null) placedCardView.enabled = false;

            _deckManager.selectedACardClick3D = null;
            _deckManager.selectedACard = null;

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
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            if (placedCardClick3D == null) yield break;
            placedCardClick3D.enabled = true;
            placedCardClick3D.isEnabled = true;
            var reflection = placedCardClick3D.GetType();
            var inputActionField =
                reflection.GetField("_mouseClickAction", BindingFlags.NonPublic | BindingFlags.Instance);
            if (inputActionField == null) yield break;
            var inputAction = inputActionField.GetValue(placedCardClick3D) as InputAction;
            if (inputAction == null) yield break;
            inputAction.Disable();
            inputAction.Dispose();
            inputActionField.SetValue(placedCardClick3D, null);
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