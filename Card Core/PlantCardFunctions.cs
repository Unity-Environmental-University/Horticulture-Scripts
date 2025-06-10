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
            var scene = SceneManager.GetActiveScene();
            if (scene.name == "Main") Destroy(this);
            deckManager = CardGameMaster.Instance.deckManager;

            if (!deckManager && SceneManager.GetActiveScene().name == "CardGame")
                Debug.LogError("deckManager not found for plant!");

            plantController = GetComponent<PlantController>();
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
        // ReSharper disable once UnusedMember.Local
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

            //if (plantController.PlantCard.GetType()) ;

            if (CardGameMaster.Instance.debuggingCardClass)
                Debug.Log(
                    $"{actionCard.Name} treatment {actionCard.Treatment?.ToString() ?? "Unknown"} applied to {plantController.name}");

            deckManager.DiscardActionCard(actionCard, true);
            Destroy(selectedCard.gameObject);
            deckManager.selectedACardClick3D = null;
        }

        public void ApplyQueuedTreatments()
        {
            var cardHolders = CardGameMaster.Instance?.cardHolders;

            if (cardHolders is null || cardHolders.Count is 0)
            {
                Debug.LogWarning("No card holders found to apply treatments.");
                return;
            }

            foreach (var cardHolder in cardHolders)
            {
                if (!cardHolder || !cardHolder.HoldingCard) continue;

                var cardView = cardHolder.placedCardView;
                if (!cardView) continue;

                var actionCard = cardView.GetCard();
                if (actionCard?.Treatment is null) continue;
                
                var parent = cardHolder.transform.parent;
                var targetPlant = parent.GetComponentInChildren<PlantController>();

                if (!targetPlant)
                {
                    Debug.LogWarning($"No PlantController found near CardHolder {cardHolder.name}");
                    continue;
                }

                actionCard.Treatment.ApplyTreatment(targetPlant);
                targetPlant.UsedTreatments.Add(actionCard.Treatment);

                if (CardGameMaster.Instance.debuggingCardClass) 
                    Debug.Log($"Applied treatment {actionCard.Treatment} from card {actionCard.Name} to Plant {targetPlant.name}");

                ClearCardHolder(cardHolder);
            }
        }
        
        private void ClearCardHolder(PlacedCardHolder cardHolder)
        {
            deckManager.DiscardActionCard(cardHolder.PlacedCard, true);
            Destroy(cardHolder.placedCardClick3D.gameObject);
            cardHolder.placedCardView = null;
            cardHolder.placedCardClick3D = null;
            cardHolder.PlacedCard = null;
            StartCoroutine(deckManager.UpdateCardHolderRenders());
        }
    }
}