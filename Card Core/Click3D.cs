using System.Collections;
using _project.Scripts.Core;
using Unity.Serialization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace _project.Scripts.Card_Core
{
    public class Click3D : MonoBehaviour
    {
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        [SerializeField] public UnityEvent onClick3D;

        [Tooltip("If True: Hovering over a card will pop it up a bit. If False: item will highlight on hover")]
        public bool handItem;

        [Tooltip("Default: 0.2 -- How fast the card pops up")]
        public float animTime = 0.2f;

        [Tooltip("Default: 1.05 -- How large the card becomes")]
        public float scaleUp = 1.05f;

        [Tooltip("Default: 0.2 -- How high the card goes")]
        public float popHeight = 0.2f;

        [DontSerialize] public bool selected;
        [DontSerialize] public CardView cardView;
        [DontSerialize] public bool isEnabled;
        
        private Color _baseColor;
        private Camera _mainCamera;
        private Mouse _mouse;
        private InputAction _mouseClickAction;
        private Renderer _objectRenderer;
        private Vector3 _originalPos;
        private Vector3 _originalScale;

        private MaterialPropertyBlock _sharedPropertyBlock;


        private void Start()
        {
            // Initialize Click3D with a True isEnabled state
            isEnabled = true;

            // Let's remove this from non-CardGame scenes
            if (SceneManager.GetActiveScene().name != "CardGame") Destroy(this);

            // If the ClickObject has a CardView, lets store it
            if (GetComponent<CardView>() && !cardView) cardView = GetComponent<CardView>();

            _objectRenderer = GetComponentInChildren<Renderer>();
            _baseColor = _objectRenderer.material.color;
            _mainCamera = Camera.main;
            _mouse = Mouse.current;
            _sharedPropertyBlock = new MaterialPropertyBlock();

            // Use localPosition so that AnimateCard and AnimateCardBack work consistently.
            _originalPos = transform.localPosition;
            _originalScale = transform.localScale;

            // Create a new action for mouse click
            _mouseClickAction = new InputAction("MouseClick", binding: "<Mouse>/leftButton");
            _mouseClickAction.performed += OnMouseClick;
            _mouseClickAction.Enable();
        }
        //public void EnableClick3D() => isEnabled = true;

        private void OnDestroy()
        {
            // Clean up when object is destroyed
            if (_mouseClickAction == null) return;
            _mouseClickAction.performed -= OnMouseClick;
            _mouseClickAction.Disable();
        }

        private void OnMouseEnter()
        {
            if (!isEnabled) return;
            if (!handItem)
            {
                _objectRenderer.material.color = Color.red; // this works for 90% of the time
                _sharedPropertyBlock.SetColor(Color1, Color.red); // this gets the rest
                _objectRenderer.SetPropertyBlock(_sharedPropertyBlock);
                return;
            }

            // Animate pop-up and scale-up
            StopAllCoroutines();
            StartCoroutine(AnimateCard());
        }

        private void OnMouseExit()
        {
            if (!isEnabled) return;
            var targetColor = _baseColor;

            // Check if this object is a plant by looking for a PlantController
            var plant = GetComponent<PlantController>();
            if (plant && plant.CurrentAfflictions is { Count: > 0 })
            {
                targetColor = plant.CurrentAfflictions[0].Color;
                plant.FlagShadersUpdate();
            }

            // Apply the determined color
            _objectRenderer.material.color = targetColor;
            _sharedPropertyBlock.SetColor(Color1, targetColor);
            _objectRenderer.SetPropertyBlock(_sharedPropertyBlock);

            if (!handItem) return;
            StopAllCoroutines();
            StartCoroutine(AnimateCardBack());
        }

        public void DisableClick3D()
        {
            isEnabled = false;
        }

        private void OnMouseClick(InputAction.CallbackContext context)
        {
            if (!isEnabled) return;
            if (_mouse == null || !_mainCamera) return;

            if (handItem)
            {
            }

            var mousePosition = _mouse.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);

            if (!Physics.Raycast(ray, out var hit) || hit.transform != transform) return;

            // Run an assigned method from the inspector
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
        /// IEnumerator used to control and yield execution for Unity's coroutine system.
        /// </returns>
        private IEnumerator AnimateCard()
        {
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
        /// IEnumerator used to manage and yield execution for Unity's coroutine system.
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
    }
}