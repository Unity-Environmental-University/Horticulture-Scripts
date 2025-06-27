using System.Collections;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class RobotCardGameSequencer : MonoBehaviour
    {
        public bool scriptingEnabled;
        [SerializeField] private RobotController robotController;
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject frontOfPlayer;

        private void Start() => StartCoroutine(BeginCardGameSequence());

        private IEnumerator BeginCardGameSequence()
        {
            robotController.currentLookTarget = player;
            robotController.GoToNewLocation(frontOfPlayer.transform.position);

            yield return new WaitUntil(robotController.HasReachedDestination);
            yield return new WaitForSeconds(3);

            robotController.animator.SetBool($"isGesturing", true);

            yield return new WaitForSeconds(robotController.animator.GetCurrentAnimatorStateInfo(0).length);

            robotController.animator.SetBool($"isGesturing", false);
        }
    }
}