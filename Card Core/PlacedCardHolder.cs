using _project.Scripts.Classes;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class PlacedCardHolder : MonoBehaviour
    {
        private DeckManager _deckManager;
        private ICard _placedCard;
        private CardView _placedCardView;

        private void Start() => _deckManager = CardGameMaster.Instance.deckManager;

        /// <summary>
        ///     Moves the currently selected card from the DeckManager to the PlacedCardHolder,
        ///     snapping its position, rotation, and scale to match the PlacedCardHolder transform.
        ///     Disables click interactions for the selected card and clears the selection in the DeckManager.
        ///     Additionally hides the renderer of the PlacedCardHolder without affecting its child objects.
        /// </summary>
        public void TakeSelectedCard()
        {
            if (_deckManager.selectedACardClick3D is null || _deckManager.SelectedACard is null) return;

            var selectedCard = _deckManager.selectedACardClick3D;

            selectedCard.DisableClick3D();

            // Set parent without preserving world values
            selectedCard.transform.SetParent(transform, false);

            // Snap to the transform exactly (position, rotation, scale)
            selectedCard.transform.localPosition = Vector3.zero;
            selectedCard.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Lying flat
            selectedCard.transform.localScale = Vector3.one; // or the original prefab scale, if different

            _deckManager.selectedACardClick3D = null;
            _deckManager.SelectedACard = null;

            // hide the parent object without hiding the children
            var parentRenderer = transform.GetComponent<Renderer>();
            if (parentRenderer) parentRenderer.enabled = false;
        }
    }
}