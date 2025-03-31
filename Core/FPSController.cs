using System.Linq;
using _project.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _project.Scripts.Core
{
    public class FPSController : MonoBehaviour
    {
        private const float BaseSpeed = 5.0f;
        private const float CrouchSpeed = 2.5f;
        private const float CrouchHeight = 1.0f;
        private const float StandHeight = 2.0f;

        [SerializeField] private GameObject fpsCamera;
        [SerializeField] private GameObject fpsPlayer;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject noteBook;
        [SerializeField] private GameObject gallery;
        [SerializeField] private ScriptedSpread scriptedSpread;
        [SerializeField] private NotebookController notebookController;
        [SerializeField] private MenuManager menuManager;
        [SerializeField] private TextMeshProUGUI currentDayText;

        public bool mouseCaptured;
        public bool lookLocked;
        public float speed = BaseSpeed;
        public float sprintMultiplier = 2f;
        public float mouseSensitivity = 2f;
        public float jumpHeight = 1.0f;


        private readonly float _gravity = Physics.gravity.y;
        private float _climbSpeed;
        private CharacterController _controller;
        private InputAction _crouchAction;
        private InputAction _escapeAction;
        private InputAction _galleryAction;
        private bool _isClimbing;
        private bool _isCrouching;
        private bool _isGrounded;
        private InputAction _jumpAction;
        private InputAction _lookAction;
        private InputAction _noteAction;
        private InputAction _progressTimeAction;
        private InputAction _sprintAction;
        private Vector3 _velocity;
        private float _xRotation;


        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            if (!menuManager) menuManager = FindFirstObjectByType<MenuManager>();
            if (!noteBook) Debug.LogError("No notebook found");

            _lookAction = InputSystem.actions.FindAction("Look");
            _escapeAction = InputSystem.actions.FindAction("Escape");
            _sprintAction = InputSystem.actions.FindAction("Sprint");
            _crouchAction = InputSystem.actions.FindAction("Crouch");
            _jumpAction = InputSystem.actions.FindAction("Jump");
            _noteAction = InputSystem.actions.FindAction("Note");
            _galleryAction = InputSystem.actions.FindAction("Gallery");
            _progressTimeAction = InputSystem.actions.FindAction("ProgressTime");
        }

        private void Update()
        {
            if (mouseCaptured || menuManager.isPaused)
                return;

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            UpdateLookDirection();
            HandleGravityEffect();
            ApplyMoveDirection();
            HandleJump();
            HandleCrouch();
            HandleSprint();
            HandleNoteBook();
            HandleTimeProgression();
            HandleGallery();
            HandleGamePause();
        }

        private void OnDestroy()
        {
            // Destroy all others
            foreach (var player in GameObject.FindGameObjectsWithTag("Player")) Destroy(player);
        }


        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Ladder")) return;
            _isClimbing = true;
            _climbSpeed = 2.0f;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Ladder")) return;
            _isClimbing = false;
        }

        private void HandleTimeProgression()
        {
            if (!_progressTimeAction.WasReleasedThisFrame()) return;
            scriptedSpread.SpreadDay(scriptedSpread.nextDay);
        }

        private void HandleGallery()
        {
            if (!_galleryAction.WasReleasedThisFrame()) return;
            switch (gallery.activeSelf)
            {
                case true:
                    CloseGallery();
                    break;
                case false:
                    OpenGallery();
                    break;
            }
        }

        private void OpenGallery()
        {
            gallery.SetActive(true);
            menuManager.isPaused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseGallery()
        {
            gallery.SetActive(false);
            if (noteBook.gameObject.activeInHierarchy) return;

            menuManager.isPaused = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        } // ReSharper disable Unity.PerformanceAnalysis
        private void UpdateLookDirection()
        {
            if (lookLocked) return;
            var lookInput = _lookAction.ReadValue<Vector2>();
            var currentInputDevice = GetCurrentDevice();
            mouseSensitivity = currentInputDevice is Gamepad ? 5f : 0.1f;

            var mouseX = lookInput.x * mouseSensitivity;
            var mouseY = lookInput.y * mouseSensitivity;
            transform.Rotate(Vector3.up * mouseX);
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
            fpsCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }

        private static object GetCurrentDevice()
        {
            InputDevice latestDevice = null;
            var latestTime = double.MinValue;

            foreach (var device in InputSystem.devices.Where(device => device.lastUpdateTime > latestTime))
            {
                latestDevice = device;
                latestTime = device.lastUpdateTime;
            }

            return latestDevice;
        }

        private void HandleGravityEffect()
        {
            _isGrounded = _controller.isGrounded;
            if (_isGrounded && _velocity.y < 0)
                _velocity.y = -2f;
        }

        private void ApplyMoveDirection()
        {
            // Calculate movement direction
            var moveX = Input.GetAxis("Horizontal");
            var moveZ = Input.GetAxis("Vertical");
            var move = transform.right * moveX + transform.forward * moveZ;

            if (move.magnitude > 1)
                move.Normalize();

            if (_isClimbing)
            {
                var verticalInput = Input.GetAxis("Vertical");
                if (verticalInput == 0) return;
                _controller.Move(transform.up * (verticalInput * _climbSpeed * Time.deltaTime));
                _velocity.y = 0;
            }
            else
            {
                _controller.Move(move * (speed * Time.deltaTime));
            }
        }

        private void HandleNoteBook()
        {
            if (!_noteAction.WasReleasedThisFrame()) return;
            switch (noteBook.activeSelf)
            {
                case true:
                    CloseNotebook();
                    break;
                case false:
                    OpenNotebook();
                    break;
            }
        }

        private void OpenNotebook()
        {
            noteBook.SetActive(true);
            menuManager.isPaused = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void CloseNotebook()
        {
            noteBook.SetActive(false);
            menuManager.isPaused = false;
            if (notebookController.IsCursorSet()) notebookController.ClearCursor();
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void HandleJump()
        {
            if (_jumpAction.IsPressed() && _isGrounded && !_isCrouching)
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * _gravity);
            if (_jumpAction.IsPressed() && _isClimbing) _isClimbing = false;
            _velocity.y += _gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        private void HandleCrouch()
        {
            if (_crouchAction.IsPressed())
            {
                _isCrouching = true;
                _controller.height = CrouchHeight;
                speed = CrouchSpeed;
            }
            else if (_crouchAction.WasReleasedThisFrame())
            {
                _isCrouching = false;
                _controller.height = StandHeight;
                speed = BaseSpeed;
            }
        }

        private void HandleSprint()
        {
            if (!_isCrouching && _isGrounded && _sprintAction.IsPressed())
                speed = BaseSpeed * sprintMultiplier;
            else if (_sprintAction.WasReleasedThisFrame())
                speed = BaseSpeed;
        }

        private void HandleGamePause()
        {
            if (_escapeAction.WasReleasedThisFrame())
                menuManager.PauseGame();
        }
    }
}