using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    ///     Defines a drop zone for draggable deck cards.
    ///     Attach to scroll content parents (actionDeckItemsParent, sideDeckItemsParent).
    ///     Works with VerticalLayoutGroup for automatic card positioning.
    /// </summary>
    public class DeckDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Color highlightColor = new(0.8f, 0.9f, 1f, 0.3f);
        [SerializeField] private bool isActionDeck;
        [SerializeField] private int minimumCards = 1;
        [SerializeField] private Transform contentParent;
        [SerializeField] private RectTransform dropArea;

        private Image _backgroundImage;
        private RectTransform _rectTransform;
        private RectTransform _contentRectTransform;
        private LayoutGroup _layoutGroup;
        private Color _originalColor;
        private bool _isHighlighted;
        
        private void Awake()
        {
            _rectTransform = dropArea != null ? dropArea : GetComponent<RectTransform>();
            _layoutGroup = GetComponent<LayoutGroup>();
            if (contentParent == null)
            {
                contentParent = transform;
            }
            _contentRectTransform = contentParent as RectTransform;
            if (_layoutGroup == null && contentParent != transform)
            {
                _layoutGroup = contentParent.GetComponent<LayoutGroup>();
            }

            // Try to find or create a background image for highlighting
            _backgroundImage = GetComponent<Image>();
            if (_backgroundImage is not null) _originalColor = _backgroundImage.color;
        }

        public void OnDrop(PointerEventData eventData)
        {
            SetHighlight(false);

            var draggable = eventData.pointerDrag?.GetComponent<DraggableCard>();
            if (draggable is null) return;

            var droppedCard = eventData.pointerDrag;
            var originalParent = draggable.OriginalParent;
            var targetParent = contentParent != null ? contentParent : transform;

            // Check if dropping would empty the action deck (must have at least minimumCards)
            if (!isActionDeck && originalParent != contentParent)
            {
                // Card is leaving another zone - check if that zone is the action deck
                var sourceZone = GetDropZoneFromTransform(originalParent);
                if (sourceZone is not null && sourceZone.isActionDeck)
                {
                    var currentActionCount = sourceZone.contentParent != null
                        ? sourceZone.contentParent.childCount
                        : sourceZone.transform.childCount;
                    // The card is temporarily reparented to canvas during drag, so the count is already reduced
                    if (currentActionCount < sourceZone.minimumCards)
                    {
                        Debug.LogWarning(
                            $"Cannot move card: Action deck must have at least {sourceZone.minimumCards} card(s).");
                        draggable.RestoreToOriginalPosition();
                        return;
                    }
                }
            }

            // Calculate insertion index based on pointer position
            var insertionIndex = CalculateInsertionIndex(
                eventData.position,
                targetParent,
                eventData.pressEventCamera);

            // Reparent card to this drop zone
            droppedCard.transform.SetParent(targetParent, false);
            droppedCard.transform.SetSiblingIndex(insertionIndex);

            // Update the draggable's origin so it knows its new home
            draggable.SetNewOrigin(targetParent, insertionIndex);

            // Force layout rebuild for immediate visual feedback
            if (_layoutGroup && _contentRectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRectTransform);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Only highlight when dragging a DraggableCard
            if (eventData.pointerDrag is null) return;
            var draggable = eventData.pointerDrag.GetComponent<DraggableCard>();
            if (!draggable) return;

            SetHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        /// <summary>
        ///     Calculates where to insert the dropped card based on the pointer Y position.
        ///     Works with vertical layout groups to insert at the appropriate index.
        /// </summary>
        private int CalculateInsertionIndex(Vector2 screenPosition, Transform targetParent, Camera eventCamera)
        {
            if (_rectTransform == null || targetParent == null) return targetParent != null
                ? targetParent.childCount
                : transform.childCount;

            var conversionCamera = eventCamera;
            if (conversionCamera == null)
            {
                var parentCanvas = _rectTransform.GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    conversionCamera = parentCanvas.worldCamera;
                }
            }

            // Convert screen position to local position in the drop zone
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                screenPosition,
                conversionCamera,
                out var localPoint);

            // Iterate through children to find insertion point
            // In a vertical layout, compare Y positions (higher Y = earlier in the list)
            var childCount = targetParent.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var child = targetParent.GetChild(i);
                if (child == null) continue;

                var childRect = child as RectTransform;
                if (childRect == null) continue;

                // If the pointer is above the center of this child, insert before it
                var childCenterY = childRect.localPosition.y;
                if (localPoint.y > childCenterY) return i;
            }

            // If we didn't find a position, add to the end
            return childCount;
        }

        private void SetHighlight(bool highlight)
        {
            if (_backgroundImage == null || _isHighlighted == highlight) return;

            _isHighlighted = highlight;
            _backgroundImage.color = highlight ? highlightColor : _originalColor;
        }

        private static DeckDropZone GetDropZoneFromTransform(Transform t)
        {
            if (!t) return null;

            // Check the transform itself and its parent (in case it's a canvas)
            var zone = t.GetComponent<DeckDropZone>();
            if (zone != null) return zone;

            zone = t.GetComponentInParent<DeckDropZone>();
            return zone != null ? zone :
                // If the card was reparented to canvas, we can't easily find its original zone
                // This case is handled by DraggableCard storing the original parent
                null;
        }
    }
}
