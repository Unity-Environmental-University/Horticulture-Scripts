using System.Collections;
using _project.Scripts.Core;
using DG.Tweening;
using Unity.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace _project.Scripts.Card_Core
{
    public class Click3D : MonoBehaviour
    {
        private static bool _click3DGloballyInitialized;
        public static bool Click3DGloballyDisabled;

        private static readonly int Color1 = Shader.PropertyToID("_Color");
        [SerializeField] public UnityEvent onClick3D;

        [Tooltip("If True: Hovering over a card will pop it up a bit. If False: item will highlight on hover")]
        public bool handItem;
        
        [Tooltip("If True: Hovering over a Sticker will scale it up a bit. If False: item will highlight on hover")]
        public bool isSticker;
        
        [DoNotSerialize] public bool isRetainedItem;

        [Tooltip("Default: 0.2 -- How fast the card pops up")]
        public float animTime = 0.2f;

        [Tooltip("Default: 1.05 -- How large the card becomes")]
        public float scaleUp = 1.05f;
        
        [Tooltip("Default: 1.5 -- How large the stickers become")]
        public float stickerScaleUp = 1.5f;

        [Tooltip("Default: 0.2 -- How high the card goes")]
        public float popHeight = 0.2f;

        [DontSerialize] public bool selected;
        [DontSerialize] public bool isEnabled;
        [DontSerialize] public bool mouseOver;
        [DontSerialize] public CardView cardView;
        private readonly Color _disabledColor = Color.grey;
        private readonly Color _hoverColor = Color.red;

        private Color _baseColor;
        private Camera _mainCamera;
        private Mouse _mouse;
        private InputAction _mouseClickAction;
        private Renderer _objectRenderer;
        private Vector3 _originalPos;
        private Vector3 _originalScale;

        private MaterialPropertyBlock _sharedPropertyBlock;
        private Material _originalMaterial;


        private void Start()
        {
            if (!_click3DGloballyInitialized)
            {
                Click3DGloballyDisabled = false;
                _click3DGloballyInitialized = true;
            }
            
            isEnabled = true;

            if (SceneManager.GetActiveScene().name != "CardGame") Destroy(this);
            if (GetComponent<CardView>() && !cardView) cardView = GetComponent<CardView>();

            _objectRenderer = GetComponentInChildren<Renderer>();
            if (_objectRenderer != null)
            {
                _originalMaterial = _objectRenderer.material;
                _baseColor = _originalMaterial.color;
                _sharedPropertyBlock = new MaterialPropertyBlock();
                _objectRenderer.GetPropertyBlock(_sharedPropertyBlock);
            }
            
            _mainCamera = Camera.main;
            _mouse = Mouse.current;

            _originalPos = transform.localPosition;
            _originalScale = transform.localScale;
            _mouseClickAction = new InputAction("MouseClick", binding: "<Mouse>/leftButton");
            _mouseClickAction.performed += OnMouseClick;
            _mouseClickAction.Enable();
        }

        /// Updates the state of the component, handling inputs and interactions during runtime.
        /// This method checks if the component is enabled and is operating in the correct scene.
        /// It specifically handles touch inputs (e.g., from mobile devices) to detect screen taps.
        /// When a touch input is detected, its position is processed to determine if the object
        /// corresponding to this component was clicked, triggering the `onClick3D` event in such cases.
        /// The update process also ensures proper behavior by verifying the necessary conditions
        /// such as the active scene and main camera availability before performing any operations.
        private void Update()
        {
            if (!isEnabled || SceneManager.GetActiveScene().name != "CardGame" || !_mainCamera 
                || Click3DGloballyDisabled)
                return;

            // Handle touch (mobile)
            if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame != true) return;
            var touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            TryClick(touchPos);
        }

        private void OnDestroy()
        {
            // Ensure any active tweens on this transform are killed to avoid Safe Mode warnings
            transform.DOKill();
            // Clean up input when the object is destroyed
            if (_mouseClickAction == null) return;
            _mouseClickAction.performed -= OnMouseClick;
            _mouseClickAction.Disable();
        }

        private void OnMouseEnter()
        {
            if (!isEnabled || Click3DGloballyDisabled) return;
            
            if (!handItem && !isSticker)
            {
                mouseOver = true;
                if (_objectRenderer == null || _sharedPropertyBlock == null) return;
                _sharedPropertyBlock.SetColor(Color1, _hoverColor);
                _objectRenderer.SetPropertyBlock(_sharedPropertyBlock);
                return;
            }

            if (isSticker)
            {
                var targetScale = _originalScale * stickerScaleUp;
                transform
                    .DOScale(targetScale, animTime)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                return;
            }
            
            StopAllCoroutines();
            StartCoroutine(AnimateCard());
        }

        private void OnMouseExit()
        {
            if (!isEnabled || Click3DGloballyDisabled) return;

            var plant = GetComponentInParent<PlantController>();
            if (plant) plant.FlagShadersUpdate();

            if (!handItem && !isSticker)
            {
                mouseOver = false;
                if (_objectRenderer == null || _sharedPropertyBlock == null) return;
                _objectRenderer.GetPropertyBlock(_sharedPropertyBlock);
                _sharedPropertyBlock.SetColor(Color1, _baseColor);
                _objectRenderer.SetPropertyBlock(_sharedPropertyBlock);
                return;
            }
            
            if (isSticker)
            {
                if (selected) return;
                transform
                    .DOScale(_originalScale, animTime)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                return;
            }
            
            StopAllCoroutines();
            StartCoroutine(AnimateCardBack());
        }


        private void TryClick(Vector2 screenPosition)
        {
            var ray = _mainCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out var hit) && hit.transform == transform) onClick3D?.Invoke();
        }

        public void RefreshState()
        {
            // Hand items (cards) don't use color changes, only animations
            if (handItem) return;
            
            if (_objectRenderer == null || _sharedPropertyBlock == null) return;

            var targetColor = isEnabled
                ? mouseOver ? _hoverColor : _baseColor
                : _disabledColor;

            _sharedPropertyBlock.SetColor(Color1, targetColor);
            _objectRenderer.SetPropertyBlock(_sharedPropertyBlock);
        }

        public void DisableClick3D() { isEnabled = false; }

        private void OnMouseClick(InputAction.CallbackContext context)
        {
            if (!isEnabled || Click3DGloballyDisabled) return;
            if (_mouse == null || !_mainCamera) return;


            var mousePosition = _mouse.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);

            if (!Physics.Raycast(ray, out var hit) || hit.transform != transform) return;

            onClick3D?.Invoke();
        }

        /// Animates a card's position and scale to create a visual "pop-up" effect.
        /// The animation smoothly transitions the card's scale and position from its
        /// original state to a defined target state, dictated by properties such as
        /// `scaleUp` and `popHeight`. It performs this interpolation over the
        /// duration specified by `animTime`.
        /// The animation will interpolate scale and position components separately
        /// to prevent exceeding target values, ensuring a natural movement.
        /// <returns>
        ///     IEnumerator used to control and yield execution for Unity's coroutine system.
        /// </returns>
        private IEnumerator AnimateCard()
        {
            if (Click3DGloballyDisabled) yield return null;
            // Define fixed target values relative to the original state.
            var targetScale = _originalScale * scaleUp;
            var targetPos = _originalPos + new Vector3(0f, popHeight, 0f);

            // Get the current state.
            var currentScale = transform.localScale;
            var currentPos = transform.localPosition;

            // Clamp the current state so it never exceeds the target.
            // For scale: if any component is already above the fixed target, use the target instead.
            var startScale = new Vector3(
                Mathf.Min(currentScale.x, targetScale.x),
                Mathf.Min(currentScale.y, targetScale.y),
                Mathf.Min(currentScale.z, targetScale.z)
            );

            // Do the same for position.
            var startPos = new Vector3(
                Mathf.Min(currentPos.x, targetPos.x),
                Mathf.Min(currentPos.y, targetPos.y),
                Mathf.Min(currentPos.z, targetPos.z)
            );

            var elapsed = 0f;
            while (elapsed < animTime)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / animTime);
                // Interpolate from the clamped start to the fixed target.
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            // Ensure the final values are exactly the target.
            transform.localScale = targetScale;
            transform.localPosition = targetPos;
        }

        /// Animates a card's scale and position back to its original state after being modified.
        /// The animation smoothly reverses from the current state of the card's scale and position
        /// to its initial state, determined by `_originalScale` and `_originalPos`. It ensures
        /// that the scale and position values are clamped to avoid transitioning below the original state.
        /// The interpolation is managed over the duration specified by `animTime`.
        /// <returns>
        ///     IEnumerator used to manage and yield execution for Unity's coroutine system.
        /// </returns>
        public IEnumerator AnimateCardBack()
        {
            if (selected) yield break;
            // Define the fixed target values to revert to.
            var targetScale = _originalScale;
            var targetPos = _originalPos;

            // Get the current state.
            var currentScale = transform.localScale;
            var currentPos = transform.localPosition;

            // Clamp the current state to ensure it doesn't animate below the original values.
            var startScale = new Vector3(
                Mathf.Max(currentScale.x, targetScale.x),
                Mathf.Max(currentScale.y, targetScale.y),
                Mathf.Max(currentScale.z, targetScale.z)
            );
            var startPos = new Vector3(
                Mathf.Max(currentPos.x, targetPos.x),
                Mathf.Max(currentPos.y, targetPos.y),
                Mathf.Max(currentPos.z, targetPos.z)
            );

            var elapsed = 0f;
            while (elapsed < animTime)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / animTime);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            transform.localScale = targetScale;
            transform.localPosition = targetPos;
        }

        /// <summary>
        /// Updates the original position and scale references used for hover animations.
        /// This should be called when the card's transform is modified externally (e.g., during hand layout changes).
        /// </summary>
        /// <param name="newScale">The new reference scale for the object to use as baseline for scaling animations</param>
        /// <param name="newPosition">The new reference position for the object to use as baseline for position animations</param>
        /// <remarks>
        /// This method allows external animation systems (like DOTween) to update the Click3D component's
        /// original transform references after layout changes. This ensures hover animations work correctly
        /// from the new baseline position and scale.
        /// </remarks>
        public void UpdateOriginalTransform(Vector3 newScale, Vector3 newPosition)
        {
            _originalScale = newScale;
            _originalPos = newPosition;
        }
    }
}
