/*
 1. Usage - Attach this to the Player Character
 2. Create the Layers for "InspectableObject" and "Large-Inspect"
 3. Attach your Player Controller script to the 'controller' param
 4. Attach all respective components to the script in the inspector

 ~Donovan Montoya
*/

using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace _project.Scripts.Core
{
    public class InspectObject : MonoBehaviour
    {
        [SerializeField] private FPSController controller;
        [SerializeField] private ScriptedRobotManager robotManager;
        [SerializeField] private Camera playerCamera;    
        [SerializeField] private GameObject overlayCamera;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private GameObject inspectImage;
        [SerializeField] private GameObject inspectImageTwo;
    
        public Color highlightColor = Color.yellow; // Color of the object when within the inspected distance
        public float inspectDistanceDefault = 2f; // Sets the Default distance
        public float rotateSpeed = 2.0f; // Speed of the object rotation
        public float highlightIntensity = 1; // Intensity of Highlight
        public bool debugging;

        private readonly Dictionary<Renderer, Color> _originalColors = new();
        private readonly Dictionary<GameObject, int> _originalLayers = new();
        private readonly Dictionary<(PlantType plantType, string Name), List<Sprite>> _plantImages = new();
    
        private InputAction _leftClickAction;
        private InputAction _leftJoyStickAction;
        private InputAction _rightClickAction;
        private InputAction _rightJoyStickAction;
        private InputAction _inspectAction;
        private DepthOfField _depthOfField;
        private GameObject _inspectableObject; // The object to inspect
        private Quaternion _originalRotation;
        private Vector3 _dragStartPosition;
        private Vector3 _originalPosition;
        private float _inspectDistance; // Distance of the object from the camera when inspected
        private bool _inspecting;
        private bool _isCheckingForHighlight = true;
        private bool _canInspect = true;
        private bool _firstInspect;

        private void Start()
        {
#if !UNITY_EDITOR
        debugging = false;
#endif
            _inspectDistance = inspectDistanceDefault;
            if (postProcessVolume.profile.TryGet<DepthOfField>(out var dof))
                _depthOfField = dof;
            _inspectAction = InputSystem.actions.FindAction("Inspect");
            _leftClickAction = InputSystem.actions.FindAction("LeftClick");
            _rightClickAction = InputSystem.actions.FindAction("RightClick");
            _leftJoyStickAction = InputSystem.actions.FindAction("LeftJoyStick");
            _rightJoyStickAction = InputSystem.actions.FindAction("RightJoyStick");

            LoadInspectImages();
        }

        private void Update()
        {
            if (_inspectAction.WasReleasedThisFrame())
            {
                // Check if the player pressed the "I" key
                if (_inspecting)
                    // Exit inspect mode
                    ExitInspectMode();
                else if (IsLookingAtLayer("InspectableObject") && IsCloseEnoughToInspect())
                    // Enter inspect mode
                    // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                    EnterInspectMode();
            }

            if (_inspecting)
            {
                HandleMouseInput();
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                HandleJoystickInput();
            }


            CheckHighlight();
        }

        private void LoadInspectImages()
        {
            // Loading plant images based on plant type and affliction
            AddPlantImage(PlantType.Coleus, new PlantAfflictions.MealyBugsAffliction(), "coleus-mealy", "coleus-mealy2");
            AddPlantImage(PlantType.Coleus, new PlantAfflictions.ThripsAffliction(), "coleus-thrips", "coleus-thrips2");
            AddPlantImage(PlantType.Coleus, new PlantAfflictions.AphidsAffliction(), "coleus-aphids", "coleus-aphids2");

            AddPlantImage(PlantType.Chrysanthemum, new PlantAfflictions.MealyBugsAffliction(), "mum-mealy", "mum-mealy2");
            AddPlantImage(PlantType.Chrysanthemum, new PlantAfflictions.ThripsAffliction(), "mum-thrips", "mum-thrips2");
            AddPlantImage(PlantType.Chrysanthemum, new PlantAfflictions.AphidsAffliction(), "mum-aphids", "mum-aphids2");
            AddPlantImage(PlantType.Chrysanthemum, new PlantAfflictions.MildewAffliction(), "mum-mildew");

            AddPlantImage(PlantType.Cucumber, new PlantAfflictions.MealyBugsAffliction(), "cucumber-mealy", "cucumber-mealy2");
            AddPlantImage(PlantType.Cucumber, new PlantAfflictions.ThripsAffliction(), "cucumber-thrips", "cucumber-thrips2");
            AddPlantImage(PlantType.Cucumber, new PlantAfflictions.AphidsAffliction(), "cucumber-aphids", "cucumber-aphids2");
            AddPlantImage(PlantType.Cucumber, new PlantAfflictions.MildewAffliction(), "cucumber-mildew");

            AddPlantImage(PlantType.Pepper, new PlantAfflictions.MealyBugsAffliction(), "pepper-mealy", "pepper-mealy2");
            AddPlantImage(PlantType.Pepper, new PlantAfflictions.ThripsAffliction(), "pepper-thrips", "pepper-thrips2");
            AddPlantImage(PlantType.Pepper, new PlantAfflictions.AphidsAffliction(), "pepper-aphids", "pepper-aphids2");
            AddPlantImage(PlantType.Pepper, new PlantAfflictions.MildewAffliction(), "pepper-mildew");
        }

        private void AddPlantImage(PlantType plantType, PlantAfflictions.IAffliction affliction, string img1, string img2 = null)
        {
            var key = (plantType, affliction.Name);
            _plantImages[key] = new List<Sprite>
            {
                Resources.Load<Sprite>($"Sprites/Plants/{img1}"),
                img2 != null ? Resources.Load<Sprite>($"Sprites/Plants/{img2}") : null
            };
        }

        private void ShowInfectImages(PlantType plantType, List<PlantAfflictions.IAffliction> afflictions)
        {
            if (debugging)
                Debug.Log($"Displaying image for {plantType} with {string.Join(", ", afflictions.Select(a => a.Name))}.");

            var imageOne = inspectImage.GetComponent<Image>();
            var imageTwo = inspectImageTwo.GetComponent<Image>();

            foreach (var affliction in afflictions)
            {
                if (!_plantImages.TryGetValue((plantType, affliction.Name), out var plantSprite)) continue;
                imageOne.sprite = plantSprite[0];
                imageTwo.sprite = plantSprite.Count > 1 ? plantSprite[1] : null;
                break;
            }

            inspectImage.SetActive(imageOne.sprite);
            inspectImageTwo.SetActive(imageTwo.sprite);
        }
    
        private void HandleMouseInput()
        {
            if (!_inspectableObject) return;
            //if the player holds the left mouse button and moves the mouse, rotate the object
            if (_leftClickAction.IsInProgress())
            {
                // Get the mouse movement
                var mouseX = Input.GetAxis("Mouse X");
                var mouseY = Input.GetAxis("Mouse Y");

                // Rotate the object
                _inspectableObject.transform.Rotate(Vector3.up, -mouseX * rotateSpeed, Space.World);
                _inspectableObject.transform.Rotate(
                    Vector3.right,
                    mouseY * rotateSpeed,
                    Space.World
                );
            }

            // if Player is holding right mouse button, move along x/y-axis
            if (_rightClickAction.IsInProgress())
            {
                var mouseX = Input.GetAxis("Mouse X");
                var mouseY = Input.GetAxis("Mouse Y");

                // Calculate movement in screen space and translate it to world space
                var moveX = playerCamera.transform.right * (mouseX * Time.unscaledDeltaTime);
                var moveY = playerCamera.transform.up * (mouseY * Time.unscaledDeltaTime);

                // Move the object without changing its distance to the camera
                _inspectableObject.transform.position += moveX + moveY;
            }


            //if the player scrolls the mouse wheel, move the object closer or further away
            if (Input.GetAxis("Mouse ScrollWheel") == 0) return;
            // Get the mouse scroll-wheel movement
            var scrollWheel = Input.GetAxis("Mouse ScrollWheel");

            // Move the object closer or further away
            _inspectableObject.transform.position +=
                playerCamera.transform.forward * scrollWheel;
        }

        private void HandleJoystickInput()
        {
            if (!_inspectableObject) return;

            // If the player moves the left joystick, rotate the object
            if (_leftJoyStickAction.IsInProgress())
            {
                // Get joystick axis values
                // ReSharper disable twice Unity.PerformanceCriticalCodeInvocation
                var joyX = _leftJoyStickAction.ReadValue<Vector2>().x;
                var joyY = _leftJoyStickAction.ReadValue<Vector2>().y;

                // Rotate the object similar to mouse movement
                _inspectableObject.transform.Rotate(Vector3.up, -joyX * rotateSpeed, Space.World);
                _inspectableObject.transform.Rotate(Vector3.right, joyY * rotateSpeed, Space.World);
            }

            // If a player moves the right joystick, move along x/y-axis
            if (!_rightJoyStickAction.IsInProgress()) return;
            {
                var joyX = _rightJoyStickAction.ReadValue<Vector2>().x;
                var joyY = _rightJoyStickAction.ReadValue<Vector2>().y;

                // Calculate movement in world space and translate the object
                var moveX = playerCamera.transform.right * (joyX * Time.unscaledDeltaTime);
                var moveY = playerCamera.transform.up * (joyY * Time.unscaledDeltaTime);

                // Move the object without changing its distance to the camera
                _inspectableObject.transform.position += moveX + moveY;
            }
        }

        //checks to see if the player is looking at the object on the raycast layer
        private bool IsLookingAtLayer(string raycastLayer)
        {
            // Create a ray from the camera to the target object
            var playerCameraTransform = playerCamera.transform;
            var cameraRay = new Ray(playerCameraTransform.position, playerCameraTransform.forward);

            // Check if the ray hits the target object
            if (!Physics.Raycast(cameraRay, out var hit)) return false;
            // _inspectDistance = hit.transform.CompareTag("Large-Inspect") ? 3.0f: inspectDistanceDefault;
            if (hit.transform.gameObject.layer != LayerMask.NameToLayer(raycastLayer)) return false;
            // Set the inspectable object
            _inspectableObject = hit.transform.gameObject;
            return true;
        }

        //check if the player is close enough to the object to inspect it
        private bool IsCloseEnoughToInspect()
        {
            // Create a ray from the camera to the target object
            var playerCameraTransform = playerCamera.transform;
            var cameraRay = new Ray(playerCameraTransform.position, playerCameraTransform.forward);

            // Check if the ray hits the target object
            if (!Physics.Raycast(cameraRay, out var hit)) return false;
            if (hit.transform.gameObject.layer != LayerMask.NameToLayer("InspectableObject")) return false;
            // Set the inspectable object
            _inspectableObject = hit.transform.gameObject;
            //check if the object is close enough to inspect
            var inspectDelta = playerCamera.transform.position - hit.transform.position;
            return inspectDelta.sqrMagnitude < _inspectDistance * _inspectDistance;
        }

        private void EnterInspectMode()
        {
            if (!_canInspect) return;
            _inspecting = true;
            // Pause and store local rotation and position
            Time.timeScale = 0;
            _originalPosition = _inspectableObject.transform.position;
            _originalRotation = _inspectableObject.transform.rotation;

            // Unlock and reveal cursor
            Cursor.lockState = CursorLockMode.None;
            controller.mouseCaptured = true;

            // Center the object's rotation pivot
            var playerCameraTransform = playerCamera.transform;
            var newRotation = Quaternion.LookRotation(playerCameraTransform.forward, playerCameraTransform.up);
            _inspectableObject.transform.rotation = newRotation;

            // Calculate the bound center of the object
            // ReSharper disabling once Unity.PerformanceCriticalCodeInvocation
            var localRenderer = _inspectableObject.GetComponent<Renderer>();
            if (localRenderer)
            {
                var boundsCenter = localRenderer.bounds.center;
                // Move the object so its center is at the camera's forward distance
                var newPosition = playerCameraTransform.position + playerCameraTransform.forward * _inspectDistance;
                var offset = newPosition - boundsCenter;
                _inspectableObject.transform.position += offset;
            }

            // Activate post-processing and overlay camera
            postProcessVolume.GameObject().SetActive(true);
            overlayCamera.SetActive(true);

            if (_depthOfField) _depthOfField = null;

            var plantController = _inspectableObject.GetComponentInChildren<PlantController>();
            var plantType = plantController.type;
            var plantAfflictions = plantController.CurrentAfflictions;

            if (plantController) ShowInfectImages(plantType, plantAfflictions);

            // Find all objects except the inspected object and its children
            foreach (var obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (obj == _inspectableObject) continue; // Skip the inspected object itself

                // Skip children of the inspected object
                if (obj.transform.IsChildOf(_inspectableObject.transform)) continue;


                // Change layer to VFX -- This is the layer the VFX are rendered onm
                // ignoring layers meant to stay invisible
                if (obj.layer == LayerMask.NameToLayer("VFX") ||
                    obj.layer == LayerMask.NameToLayer("Ignore Raycast") ||
                    obj.layer == LayerMask.NameToLayer("PlayerBody") ||
                    obj.layer == LayerMask.NameToLayer("DontRender"))
                    continue;
                _originalLayers[obj] = obj.layer;
                obj.layer = LayerMask.NameToLayer("VFX");
            }
        }

        private void ExitInspectMode()
        {
            if (!_firstInspect)
            {
                robotManager.SetFlag(ScriptFlags.Inspected);
                _firstInspect = true;
            }
            // Cleanup
            _inspecting = false;
            Time.timeScale = 1;
            _inspectableObject.transform.position = _originalPosition;
            _inspectableObject.transform.rotation = _originalRotation;
            inspectImage.SetActive(false);
            inspectImageTwo.SetActive(false);

            // re-lock and re-hide cursor
            Cursor.lockState = CursorLockMode.Locked;

            controller.mouseCaptured = false;

            //set inspectableObject to null
            _inspectableObject = null;

            //re-enable check for highlight
            _isCheckingForHighlight = true;

            if (postProcessVolume.isActiveAndEnabled) postProcessVolume.GameObject().SetActive(false);
            if (overlayCamera.activeSelf) overlayCamera.SetActive(false);
            if (_depthOfField) _depthOfField.active = false;

            // **Restore the original layers of all objects**
            foreach (var kvp in _originalLayers) kvp.Key.layer = kvp.Value; // **Restore to original layer**
            _originalLayers.Clear();
        }

        //checks to see if the player is looking at the object on the raycast layer
        // ReSharper disable Unity.PerformanceAnalysis
        private void CheckHighlight()
        {
            try
            {
                //highlight only when player is looking at the object and remove highlight when player is not looking at the object
                if (_isCheckingForHighlight)
                {
                    if (IsLookingAtLayer("InspectableObject") && IsCloseEnoughToInspect())
                    {
                        //highlight the object and its children
                        HighlightObject(_inspectableObject, highlightColor);
                    }
                    else
                    {
                        //remove highlight from the object and its children -- if statement to prevent call on a non-set object
                        if (_inspectableObject) RestoreOriginalColors(_inspectableObject);
                        return;
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }

            //if the player is inspecting the object, stop checking for highlight and remove highlight
            if (!_inspecting) return;
            _isCheckingForHighlight = false;
            RestoreOriginalColors(_inspectableObject);
        }

        private void HighlightObject(GameObject obj, Color color)
        {
            if (!obj) return;

            // Change the color of the current object if it has a Renderer
            var objectRenderer = obj.GetComponent<Renderer>();
            if (objectRenderer)
            {
                // Remember the original color
                if (!_originalColors.ContainsKey(objectRenderer))
                    _originalColors[objectRenderer] = objectRenderer.sharedMaterial.color;
                // Make the highlight color brighter
                objectRenderer.sharedMaterial.color = color * highlightIntensity;
            }

            // Recursively change color of all child objects
            foreach (Transform child in obj.transform) HighlightObject(child.gameObject, color);
        }

        public void ToggleSearch(bool value)
        {
            _canInspect = value;
        }

        private void RestoreOriginalColors(GameObject obj)
        {
            if (!obj) return;

            // Restore the color of the current object if it has a Renderer
            var objectRenderer = obj.GetComponent<Renderer>();
            if (objectRenderer && _originalColors.TryGetValue(objectRenderer, out var originalColor))
                objectRenderer.sharedMaterial.color = originalColor;

            // Recursively restore the color of all child objects
            foreach (Transform child in obj.transform) RestoreOriginalColors(child.gameObject);
        }
    }
}