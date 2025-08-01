using System.Collections;
using _project.Scripts.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _project.Scripts.Handlers
{
    public class InteractWithRobot : MonoBehaviour
    {
        [SerializeField] private FPSController controller;
        [SerializeField] private ScriptedRobotManager robotManager;
        [SerializeField] private GameObject interactHUD;

        public Camera playerCamera;
        public float interactDistance = 5f;
        public int interactDialogue;

        private const string RaycastLayer = "Interactable";
        private InputAction _interactAction;

        private void Start()
        {
            _interactAction = InputSystem.actions.FindAction("Interact");
        }

        private void Update()
        {
            if (!robotManager.CheckFlag(ScriptFlags.HasInteract)) return;
            if (IsValidInteractionTarget())
            {
                if (!interactHUD.activeSelf) interactHUD.SetActive(true);
                if (_interactAction.WasReleasedThisFrame())
                    Interact();
            }
            else
            {
                if (interactHUD.activeSelf) interactHUD.SetActive(false);
            }
        }

        private bool IsValidInteractionTarget()
        {
            var playerCameraTransform = playerCamera.transform;
            var cameraRay = new Ray(playerCameraTransform.position, playerCameraTransform.forward);

            // Check if the ray hits an object on the specified layer, and it's close enough to interact with
            if (!Physics.Raycast(cameraRay, out var hit)) return false;
            if (hit.transform.gameObject.layer != LayerMask.NameToLayer(RaycastLayer)) return false;
            //check if the object is close enough to inspect
            var interactDelta = playerCamera.transform.position - hit.transform.position;
            return interactDelta.sqrMagnitude < interactDistance * interactDistance;
        }


        // ReSharper disable Unity.PerformanceAnalysis
        private void Interact()
        { 
            // Robot should tell the player they need to do a thing to end the day
            //robotManager.ClearFlag(ScriptFlags.HasInteract);
            interactHUD.SetActive(false);
            robotManager.SetDialogueText(interactDialogue, 1);
            robotManager.OpenDialogueBox();
            StartCoroutine(TemporaryCloseDelayCoroutine()); // replace it with dialogue audio timing.
        }

        private IEnumerator TemporaryCloseDelayCoroutine()
        {
            yield return new WaitForSeconds(5);
            StartCoroutine(robotManager.CloseDialogueBox());
        }
    }
}