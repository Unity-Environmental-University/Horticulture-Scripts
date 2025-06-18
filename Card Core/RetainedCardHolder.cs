using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class RetainedCardHolder : MonoBehaviour
    {
        private DeckManager _deckManager;
        private GameObject cardGoClone;
        public ICard HeldCard { get; private set; }

        private bool HasCard => HeldCard != null;

        private void Start() { _deckManager = CardGameMaster.Instance.deckManager; }

        public void HoldSelectedCard()
        {
            if (HasCard) { SelectHeldCard(); return; }

            // Get the selected ICard and Set the Held Card To It
            var selectedICard = _deckManager.SelectedACard;
            HeldCard = selectedICard;

            // Get the selected Click3D and Hold it
            var selectedCard3D = _deckManager.selectedACardClick3D;
            cardGoClone = Instantiate(selectedCard3D.gameObject, transform);

            // Set up the CloneCardView To Visualize the Card
            var cardViewClone = cardGoClone.GetComponent<CardView>();
            if (cardViewClone)
                cardViewClone.Setup(_deckManager.SelectedACard);

            // Set parent without preserving world values
            cardGoClone.transform.SetParent(transform, false);

            // Snap to the transform exactly (position, rotation, scale)
            cardGoClone.transform.localPosition = Vector3.zero;
            cardGoClone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            cardGoClone.transform.localScale = Vector3.one * 0.9f;

            // Clear the DeckManager
            _deckManager.selectedACardClick3D = null;
            _deckManager.SelectedACard = null;

            // Hide the CardButton and the card in Hand
            var buttonRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (buttonRenderer)
                buttonRenderer.enabled = false;
            var cardRenderers = selectedCard3D.GetComponentsInChildren<Renderer>();
            if (cardRenderers == null) return;
            foreach (var renderer1 in cardRenderers) renderer1.enabled = false;
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
            cardGoClone = returnedCardGo;
            HeldCard = returnedCardGo.GetComponent<CardView>()?.GetCard();

            var click3D = cardGoClone.GetComponent<Click3D>();
            var cardView = cardGoClone.GetComponent<CardView>();

            if (cardView)
                cardView.Setup(HeldCard);

            if (click3D)
            {
                click3D.cardView = cardView;
                click3D.isEnabled = true;
                click3D.selected = false;
                click3D.RefreshState();
            }

            // Activate hierarchy
            var current = cardGoClone.transform;
            while (current != null)
            {
                current.gameObject.SetActive(true);

                var pr = current.GetComponent<Renderer>();
                if (pr) pr.enabled = true;

                current = current.parent;
            }

            foreach (var r in cardGoClone.GetComponentsInChildren<Renderer>(true)) r.enabled = true;

            cardGoClone.transform.SetParent(transform, false);
            cardGoClone.transform.localPosition = Vector3.zero;
            cardGoClone.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            cardGoClone.transform.localScale = Vector3.one * 0.9f;
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