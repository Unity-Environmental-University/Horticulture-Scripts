using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class PlacedCardHolder : MonoBehaviour
    {
        private DeckManager _deckManager;
        private ScoreManager _scoreManager;

        public Click3D placedCardClick3D;
        public CardView placedCardView;
        public ICard PlacedCard;
        public bool HoldingCard => placedCardClick3D;

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
            _scoreManager = CardGameMaster.Instance.scoreManager;
        }

        /// <summary>
        /// Handles the selection of a card and attaches it to the cardholder, effectively "placing" the card.
        /// If a card is already placed, it will be returned to its original position before placing the selected one.
        /// </summary>
        /// <remarks>
        /// This method:
        /// 1. Checks if a card is currently being held:
        /// - If yes, the existing card is returned to its original position.
        /// 2. Verify if a card is selected on the deck. If no card is selected, the method exits without action.
        /// 3. Clones the selected card and its associated components, then places it in the cardholder:
        /// - The cloned card is configured to replace the selected card.
        /// - Its transform properties (position, rotation, scale) are updated for proper appearance.
        /// 4. Updates internal references to track the new "placed" card.
        /// 5. Hides the associated 3D click capability and visual appearance of the original card in the deck.
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

            if (PlacedCard.Value != null) _scoreManager.treatmentCost += PlacedCard.Value.Value;
            _scoreManager.CalculateTreatmentCost();
        }

        /// <summary>
        /// Returns the currently placed card to the action card parent in the deck,
        /// re-enabling its rendering and interaction capabilities while removing the cloned card from the holder.
        /// </summary>
        /// <remarks>
        /// This method performs the following actions:
        /// 1. Checks if the holder is currently holding a card.
        /// - If no card is present, the method exits without action.
        /// 2. Iterates through all card views in the action card parent to locate the originally placed card:
        /// - Enables all associated renderers to make the card visible again.
        /// - Re-enables the 3D click interaction system on the card, resets its selection state, and animates it back to its original position.
        /// 3. Destroy the cloned card held in the holder.
        /// 4. Reset internal references associated with the placed card, clearing the holder state.
        /// 5. Re-enables the visual appearance of the holder UI, such as button or parent object rendering, to indicate it is no longer holding a card.
        /// </remarks>
        private void GiveBackCard()
        {
            if (!HoldingCard) return;

            // Re-enable the original card in the hand
            var plants = _deckManager.actionCardParent.GetComponentsInChildren<CardView>(true);
            foreach (var cardView in plants)
                if (cardView.GetCard() == PlacedCard)
                {
                    var renderers = cardView.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer1 in renderers) renderer1.enabled = true;
                    var click3D = cardView.GetComponent<Click3D>();
                    if (click3D)
                    {
                        click3D.isEnabled = true;
                        click3D.selected = false;
                        click3D.RefreshState();
                        click3D.StartCoroutine(click3D.AnimateCardBack());
                    }

                    break;
                }

            Destroy(placedCardClick3D.gameObject);
            
            if (PlacedCard.Value != null) _scoreManager.treatmentCost -= PlacedCard.Value.Value;
            _scoreManager.CalculateTreatmentCost();

            // Clear the current PlacedCardHolder references
            placedCardView = null;
            placedCardClick3D = null;
            PlacedCard = null;

            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer)
                buttonRenderer.enabled = true;

            // Show the parent object renderer again
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