using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    ///     Enables drag-and-drop functionality for deck card items.
    ///     Attach to each card prefab that should be draggable between deck zones.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float dragAlpha = 0.7f;

        private CanvasGroup _canvasGroup;
        private Vector2 _originalAnchoredPosition;

        // Original state for restoration on a failed drop
        private int _originalSiblingIndex;

        // Parent scroll rect to disable during drag
        private ScrollRect _parentScrollRect;
        private bool _wasParentScrollRectEnabled;
        private RectTransform _rectTransform;
        private Canvas _rootCanvas;

        /// <summary>
        ///     The original parent transform before drag started.
        ///     Used by DeckDropZone to determine source zone for validation.
        /// </summary>
        public Transform OriginalParent { get; private set; }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Store original state for restoration
            OriginalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();
            _originalAnchoredPosition = _rectTransform.anchoredPosition;

            // Find and cache the root canvas for proper render order during drag
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

            // Disable parent ScrollRect to prevent scroll conflicts during drag
            _parentScrollRect = GetComponentInParent<ScrollRect>();
            if (_parentScrollRect != null)
            {
                _wasParentScrollRectEnabled = _parentScrollRect.enabled;
                _parentScrollRect.enabled = false;
            }

            // Visual feedback: semi-transparent and don't block raycasts
            _canvasGroup.alpha = dragAlpha;
            _canvasGroup.blocksRaycasts = false;

            // Reparent to root canvas for proper render order (on top of everything)
            if (_rootCanvas != null) transform.SetParent(_rootCanvas.transform, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Follow the pointer position
            if (_rootCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint);

                _rectTransform.localPosition = localPoint;
            }
            else
            {
                // Fallback: use world position
                _rectTransform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Restore visual state
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            // Re-enable parent scroll rect
            if (_parentScrollRect != null) _parentScrollRect.enabled = _wasParentScrollRectEnabled;

            // If we weren't dropped on a valid DeckDropZone, restore to original position
            // The DeckDropZone.OnDrop handles successful drops by reparenting before this is called
            if (transform.parent == _rootCanvas?.transform) RestoreToOriginalPosition();
        }

        /// <summary>
        ///     Restores the card to its original parent and position.
        ///     Called when dropped outside a valid drop zone.
        /// </summary>
        public void RestoreToOriginalPosition()
        {
            if (OriginalParent is null) return;
            transform.SetParent(OriginalParent, false);
            transform.SetSiblingIndex(_originalSiblingIndex);
            _rectTransform.anchoredPosition = _originalAnchoredPosition;
        }

        /// <summary>
        ///     Called by DeckDropZone when this card is successfully dropped.
        ///     Updates the original parent reference so future drags start from the new location.
        /// </summary>
        public void SetNewOrigin(Transform newParent, int siblingIndex)
        {
            OriginalParent = newParent;
            _originalSiblingIndex = siblingIndex;
        }
    }
}
