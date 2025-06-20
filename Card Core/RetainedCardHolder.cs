using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class RetainedCardHolder : MonoBehaviour
    {
        [SerializeField] private GameObject cardPrefab;
        private DeckManager _deckManager;
        private GameObject cardGoClone;
        public ICard HeldCard { get; private set; }

        private bool HasCard => HeldCard != null;

        private void Start()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
        }

        public void HoldSelectedCard()
        {
            var selectedCard = _deckManager.SelectedACard;
            var selectedClick3D = _deckManager.selectedACardClick3D;

            if (selectedCard == null || selectedClick3D == null)
            {
                if (HasCard)
                    SelectHeldCard();
                return;
            }

            // Check if already holding card
            if (HasCard && HeldCard != selectedCard)
                return;

            if (HeldCard == selectedCard)
            {
                SelectHeldCard();
                return;
            }

            // Set the stored data
            HeldCard = selectedCard;

            cardGoClone = Instantiate(cardPrefab, transform);
            cardGoClone.name = cardPrefab.name;

            // Setup visuals
            var cardView = cardGoClone.GetComponent<CardView>();
            if (cardView)
                cardView.Setup(HeldCard);

            // Setup interaction logic
            var click3D = cardGoClone.GetComponent<Click3D>();
            if (click3D)
            {
                click3D.isStoredItem = true;
                click3D.cardView = cardView;
                click3D.isEnabled = true;
                click3D.selected = false;
                click3D.RefreshState();
            }

            // Final visual positioning
            cardGoClone.transform.SetParent(transform, false);
            cardGoClone.transform.localPosition = Vector3.zero;
            cardGoClone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            cardGoClone.transform.localScale = Vector3.one * 0.9f;

            // Hide the original card in the hand
            foreach (var r in selectedClick3D.GetComponentsInChildren<Renderer>())
                r.enabled = false;
            selectedClick3D.isEnabled = false;

            // Clear selection
            _deckManager.SelectedACard = null;
            _deckManager.selectedACardClick3D = null;

            // Hide retained slot button
            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer)
                buttonRenderer.enabled = false;

            // Update cost
            var scoreManager = CardGameMaster.Instance.scoreManager;
            if (HeldCard.Value != null)
                scoreManager.treatmentCost += HeldCard.Value.Value;
            scoreManager.CalculateTreatmentCost();
        }

        private void SelectHeldCard()
        {
            var click3D = cardGoClone.GetComponent<Click3D>();
            if (click3D == null) return;

            if (_deckManager.SelectedACard == HeldCard)
            {
                _deckManager.selectedACardClick3D = null;
                _deckManager.SelectedACard = null;
                click3D.selected = false;
                StartCoroutine(click3D.AnimateCardBack());
            }
            else
            {
                if (_deckManager.selectedACardClick3D)
                {
                    _deckManager.selectedACardClick3D.selected = false;
                    StartCoroutine(_deckManager.selectedACardClick3D.AnimateCardBack());
                }

                ShowCard();

                click3D.selected = true;
                click3D.RefreshState();

                _deckManager.SelectedACard = HeldCard;
                _deckManager.selectedACardClick3D = click3D;
            }
        }

        private void ShowCard()
        {
            if (cardGoClone == null) return;
            foreach (var renderer1 in cardGoClone.GetComponentsInChildren<Renderer>())
                renderer1.enabled = true;
        }

        public void ReclaimCard(GameObject returnedCardGo)
        {
            // Safely destroy the old retained visual if it exists
            if (cardGoClone != null)
            {
                Destroy(cardGoClone);
                cardGoClone = null;
            }

            // Get the card data from the returned visual
            var returnedCardView = returnedCardGo.GetComponent<CardView>();
            HeldCard = returnedCardView?.GetCard();

            if (cardPrefab == null)
            {
                Debug.LogWarning("Held card has no valid prefab.");
                return;
            }

            // Create a clean visual from prefab
            cardGoClone = Instantiate(cardPrefab, transform);
            cardGoClone.name = cardPrefab.name;

            // Setup components
            var cardView = cardGoClone.GetComponent<CardView>();
            if (cardView) cardView.Setup(HeldCard);

            var click3D = cardGoClone.GetComponent<Click3D>();
            if (click3D)
            {
                click3D.cardView = cardView;
                click3D.isEnabled = true;
                click3D.selected = false;
                click3D.isStoredItem = true;
                click3D.RefreshState();
            }

            cardGoClone.transform.SetParent(transform, false);
            cardGoClone.transform.localPosition = Vector3.zero;
            cardGoClone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            cardGoClone.transform.localScale = Vector3.one * 0.9f;

            Destroy(returnedCardGo);
        }

        public void ClearHeldCard()
        {
            HeldCard = null;

            if (cardGoClone)
            {
                Destroy(cardGoClone);
                cardGoClone = null;
            }

            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer)
                buttonRenderer.enabled = true;
        }
    }
}