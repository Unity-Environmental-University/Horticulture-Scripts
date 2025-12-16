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

        private string _clipName;
        private bool _uiResumed;

        private void Start() => StartCoroutine(BeginCardGameSequence());

        private IEnumerator BeginCardGameSequence()
        {
            // Always start the UI Pop-in animation, then pause it so it stays off-screen
            // until gameplay signals it's time to show (drawing the hand).
            uiAnimator.Play();
            _clipName = uiAnimator.clip.name;
            uiAnimator[_clipName].speed = 0;

            if (!CardGameMaster.IsSequencingEnabled) yield break;

            robotController.currentLookTarget = robotCameraObj;
            robotController.GoToNewLocation(frontOfPlayer.transform.position);

            yield return new WaitUntil(robotController.HasReachedDestination);

            // Wait until the Timeline has concluded; do NOT resume the UI yet.
            var turnController = CardGameMaster.Instance.turnController;
            yield return new WaitUntil(turnController.readyToPlay =
                () =>
                {
                    var director = CinematicDirector.Director;
                    if (!director)
                        return true;
                    return director.state != PlayState.Playing;
                });
            robotController.currentLookTarget = player;
        }

        /// <summary>
        ///     Resume the paused UI pop-in animation, revealing UI elements (e.g., buttons) on-screen.
        ///     Safe to call multiple times; subsequent calls are ignored after first resume.
        /// </summary>
        private void ResumeUIPopIn()
        {
            if (_uiResumed || !uiAnimator || string.IsNullOrEmpty(_clipName)) return;
            var state = uiAnimator[_clipName];
            if (state == null) return;
            state.speed = 1f;
            _uiResumed = true;
        }

        /// <summary>
        ///     Resumes the UI pop-in animation (if not already resumed) and waits until it completes.
        /// </summary>
        public IEnumerator ResumeUIPopInAndWait()
        {
            Debug.Log("[RobotCardGameSequencer] ResumeUIPopInAndWait called.");
            ResumeUIPopIn();

            if (!uiAnimator)
            {
                Debug.LogWarning("[RobotCardGameSequencer] No uiAnimator found, skipping UI animation.");
                yield break;
            }

            Debug.Log($"[RobotCardGameSequencer] UI animator found. isPlaying={uiAnimator.isPlaying}");

            // Ensure a frame passes so Animation starts playing after speed change
            yield return null;

            Debug.Log($"[RobotCardGameSequencer] After frame wait. isPlaying={uiAnimator.isPlaying}");

            // Wait until the animator finishes the current clip
            var frameCount = 0;
            while (uiAnimator.isPlaying)
            {
                frameCount++;
                if (frameCount % 60 == 0) // Log every 60 frames (roughly every second at 60 FPS)
                {
                    Debug.Log($"[RobotCardGameSequencer] Still waiting for UI animation... (frame {frameCount})");
                }
                yield return null;
            }

            Debug.Log($"[RobotCardGameSequencer] UI animation complete after {frameCount} frames.");
        }
    }
}
