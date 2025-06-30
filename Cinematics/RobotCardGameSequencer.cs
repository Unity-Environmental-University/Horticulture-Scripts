using System.Collections;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Cinematics
{
    public class RobotCardGameSequencer : MonoBehaviour
    {
        [SerializeField] private RobotController robotController;
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject frontOfPlayer;
        public bool scriptingEnabled;

        private void Start() => StartCoroutine(BeginCardGameSequence());

        private IEnumerator BeginCardGameSequence()
        {
            if (!scriptingEnabled) yield break;
            
            robotController.currentLookTarget = player;
            robotController.GoToNewLocation(frontOfPlayer.transform.position);

            yield return new WaitUntil(robotController.HasReachedDestination);

            robotController.animator.SetBool($"isGesturing", true);

            yield return new WaitForSeconds(robotController.animator.GetCurrentAnimatorStateInfo(0).length);

            robotController.animator.SetBool($"isGesturing", false);
        }
    }
}