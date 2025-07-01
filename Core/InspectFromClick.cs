using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Data;
using _project.Scripts.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace _project.Scripts.Core
{
    public class InspectFromClick : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private GameObject overlayCamera;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private float inspectDistance = 1.5f;
        
        private static readonly Dictionary<GameObject, int> OriginalLayers = new();
        
        private static DepthOfField _depthOfField;
        private static GameObject _inspectableObject;
        private static GameObject[] _objectCache;
        private static Vector3 _originalPosition;
        private static Quaternion _originalRotation;

        private void Awake()
        {
            if (!playerCamera) playerCamera = Camera.main;
            if (!overlayCamera) overlayCamera = playerCamera!.GetComponent<CameraTweaks>().overlayCamera;

            if (!postProcessVolume)
                postProcessVolume = CardGameMaster.Instance.postProcessVolume;
        }

        private void Update()
        {
            if (postProcessVolume.profile.TryGet<DepthOfField>(out var dof))
                _depthOfField = dof;
        }

        public void ToggleInspect()
        {
            if (!CardGameMaster.Instance.isInspecting)
                EnterInspectMode();
            else
                ExitInspectMode();
        }

        private void EnterInspectMode()
        {
            _inspectableObject = gameObject;
            CardGameMaster.Instance.isInspecting = true;
            CardGameMaster.Instance.turnController.canClickEnd = false;
            CardGameMaster.Instance.inspectingInfoPanels.SetActive(true);

            // Store local rotation and position
            _originalPosition = _inspectableObject.transform.position;
            _originalRotation = _inspectableObject.transform.rotation;

            // Deactivate game objects with tag 'Card'
            _objectCache = GameObject.FindGameObjectsWithTag("Card");
            foreach (var obj in _objectCache) obj.SetActive(false);

            // Center the object's rotation pivot
            var playerCameraTransform = playerCamera.transform;
            var newRotation = Quaternion.LookRotation(playerCameraTransform.forward, playerCameraTransform.up);

            // Flip the rotation 180 degrees
            newRotation *= Quaternion.Euler(0, 180, 0);

            _inspectableObject.transform.rotation = newRotation;

            // Calculate the bound center of the object
            var localRenderer = _inspectableObject.GetComponent<Renderer>();
            if (localRenderer)
            {
                var boundsCenter = localRenderer.bounds.center;
                // Move the object so its center is at the camera's forward distance
                var newPosition = playerCameraTransform.position + playerCameraTransform.forward * inspectDistance;
                var offset = newPosition - boundsCenter;
                _inspectableObject.transform.position += offset;
            }

            // Activate post-processing and overlay camera
            postProcessVolume.GameObject().SetActive(true);
            overlayCamera.SetActive(true);

            // if (_depthOfField) _depthOfField = null;
            if (_depthOfField) _depthOfField.active = true;

            var plantController = _inspectableObject.GetComponentInChildren<PlantController>();
            if (plantController != null)
            {
                var fetcher = CardGameMaster.Instance.inspectingInfoPanels.GetComponentInChildren<SiteDataFetcher>();
                if (fetcher != null)
                    fetcher.SetPlant(plantController.type);
            }

            // Find all objects except the inspected object and its children
            foreach (var obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (obj == _inspectableObject) continue; // Skip the inspected object itself

                // Skip children of the inspected object
                if (obj.transform.IsChildOf(_inspectableObject.transform)) continue;

                // Change layer to VFX -- This is the layer the VFX are rendered on
                // ignoring layers meant to stay invisible
                if (obj.layer == LayerMask.NameToLayer("VFX") ||
                    obj.layer == LayerMask.NameToLayer("Ignore Raycast") ||
                    obj.layer == LayerMask.NameToLayer("PlayerBody") ||
                    obj.layer == LayerMask.NameToLayer("DontRender") ||
                    obj.layer == LayerMask.NameToLayer("CardUI"))
                    continue;
                OriginalLayers[obj] = obj.layer;
                obj.layer = LayerMask.NameToLayer("VFX");
            }
        }

        private void ExitInspectMode()
        {
            // Cleanup
            CardGameMaster.Instance.isInspecting = false;
            CardGameMaster.Instance.turnController.canClickEnd = true;
            CardGameMaster.Instance.inspectingInfoPanels.SetActive(false);
            _inspectableObject.transform.position = _originalPosition;
            _inspectableObject.transform.rotation = _originalRotation;

            // Reactivate 'Card' Objects
            foreach (var obj in _objectCache) obj.SetActive(true);

            //set inspectableObject to null
            _inspectableObject = null;

            if (postProcessVolume.isActiveAndEnabled) postProcessVolume.GameObject().SetActive(false);
            if (overlayCamera.activeSelf) overlayCamera.SetActive(false);
            if (_depthOfField) _depthOfField.active = false;

            // Restore the original layers of all objects, skipping any that have been destroyed
            foreach (var kvp in OriginalLayers.Where(kvp => kvp.Key != null))
            {
                kvp.Key.layer = kvp.Value;
            }
            OriginalLayers.Clear();
        }
    }
}