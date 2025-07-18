using System.Collections;
using _project.Scripts.Card_Core;
using _project.Scripts.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace _project.Scripts.Cinematics
{
    public class RobotCardGameSequencer : MonoBehaviour
    {
        [SerializeField] private RobotController robotController;
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject frontOfPlayer;
        [SerializeField] private Animation uiAnimator;
        [SerializeField] private GameObject robotCameraObj;

        private void Start() => StartCoroutine(BeginCardGameSequence());

        private IEnumerator BeginCardGameSequence()
        {
            if (!CardGameMaster.IsSequencingEnabled)
            {
                uiAnimator.Play();
                yield break;
            }
            
            // Start UI Pop-in animation to put the objects off-screen, then pause it
            uiAnimator.Play();
            var clipName = uiAnimator.clip.name;
            uiAnimator[clipName].speed = 0;
            
            robotController.currentLookTarget = robotCameraObj;
            robotController.GoToNewLocation(frontOfPlayer.transform.position);

            yield return new WaitUntil(robotController.HasReachedDestination);
            
            // Wait until the Timeline has concluded, then resume the UI Pop-in animation
            var turnController = CardGameMaster.Instance.turnController;
            yield return new WaitUntil(turnController.ReadyToPlay =
                () => CinematicDirector.director.state != PlayState.Playing);
            uiAnimator[clipName].speed = 1;
            robotController.currentLookTarget = player;
        }
    }
}