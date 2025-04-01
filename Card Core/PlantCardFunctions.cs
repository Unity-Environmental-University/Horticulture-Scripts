using _project.Scripts.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _project.Scripts.Card_Core
{
    public class PlantCardFunctions : MonoBehaviour
    {
        public DeckManager deckManager;
        public PlantController plantController;

        private void Start()
        {
            deckManager = CardGameMaster.Instance.deckManager;

            if (!deckManager && SceneManager.GetActiveScene().name == "CardGame")
                Debug.LogError("deckManager not found for plant!");

            plantController = GetComponent<PlantController>();

            var click3D = GetComponentInChildren<Click3D>();
            if (click3D) click3D.onClick3D.AddListener(ApplyTreatment);
        }

        /// <summary>
        ///     Applies a treatment to the plant controlled by this instance.
        ///     This method is triggered by a 3D click event on the card in the game.
        ///     It checks if there is a selected card from the deck, validates the card,
        ///     and applies its treatment to the PlantController associated with this instance.
        ///     The method also adds the treatment to the list of used treatments for the plant,
        ///     discards the used card from the deck's action pile, and destroys the
        ///     associated card GameObject.
        /// </summary>
        /// <remarks>
        ///     If no card is selected or the selected card is invalid, the method logs warnings
        ///     and exits early without applying any treatment. Expected exceptions are handled
        ///     using null checks and warnings.
        /// </remarks>
        private void ApplyTreatment()
        {
            var selectedCard = deckManager.selectedACardClick3D;
            if (!selectedCard || !selectedCard.gameObject)
            {
                Debug.LogWarning("No selected card found or card is invalid.");
                return;
            }

            var cardView = selectedCard.GetComponent<CardView>();
            if (!cardView)
            {
                Debug.LogWarning("Selected card does not contain a CardView component.");
                return;
            }

            var actionCard = cardView.GetCard();
            if (actionCard?.Treatment == null)
            {
                Debug.LogWarning("Selected card does not contain a valid treatment.");
                return;
            }

            actionCard.Treatment.ApplyTreatment(plantController);

            if (actionCard.Treatment != null) plantController.UsedTreatments.Add(actionCard.Treatment);

            Debug.Log(
                $"{actionCard.Name} treatment {actionCard.Treatment?.ToString() ?? "Unknown"} applied to {plantController.name}");

            deckManager.DiscardActionCard(actionCard, true);
            Destroy(selectedCard.gameObject);
            deckManager.selectedACardClick3D = null;
        }

        public void ApplyQueuedTreatments()
        {
            // TODO -- Re-Write this
            // Get all cardholders from the CardGameMaster instance
            var cardHolders = CardGameMaster.Instance?.cardHolders;

            if (cardHolders == null || cardHolders.Count == 0)
            {
                Debug.LogWarning("No card holders found to apply treatments.");
                return;
            }

            // Iterate through all cardholders
            foreach (var cardHolder in cardHolders)
            {
                // Null-check the cardholder object
                if (cardHolder == null)
                {
                    Debug.LogWarning("Encountered a null card holder.");
                    continue;
                }

                // Check if the cardholder is holding a card and validate
                if (!cardHolder.HoldingCard) continue;
                var cardView = cardHolder.placedCardView;
                if (!cardView)
                {
                    Debug.LogWarning($"Card holder {cardHolder.name} is holding a card, but the CardView is null.");
                    continue;
                }

                // Retrieve the action card and its treatment
                var actionCard = cardView.GetCard();
                if (actionCard == null)
                {
                    Debug.LogWarning($"Card view in holder {cardHolder.name} has no associated card.");
                    continue;
                }

                var treatment = actionCard.Treatment;
                if (treatment == null)
                {
                    Debug.LogWarning(
                        $"Card {actionCard.Name} in holder {cardHolder.name} has no treatment to apply.");
                    continue;
                }

                // Apply the treatment to the plant controller
                treatment.ApplyTreatment(plantController);

                // Log and track applied treatments
                plantController.UsedTreatments.Add(treatment);
                Debug.Log(
                    $"Applied treatment {treatment} from card {actionCard.Name} to Plant {plantController.name}.");
            }
        }
    }
}