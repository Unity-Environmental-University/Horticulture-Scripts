using _project.Scripts.Card_Core;
using UnityEngine;
using UnityEngine.Playables;

namespace _project.Scripts.Cinematics
{
    public class CinematicDirector : MonoBehaviour
    {
        public static PlayableDirector Director;
        public PlayableAsset introTimeline;
        public PlayableAsset aphidsTimeline;
        public PlayableAsset postAphidsTimeline;

        private void Awake()
        {
            if (Director == null)
                Director = FindFirstObjectByType<PlayableDirector>();
            var gm = CardGameMaster.Instance;
            if (gm?.turnController != null)
                gm.turnController.readyToPlay = () => Director != null && Director.state != PlayState.Playing;
        }

        private void Start()
        {
            if (CardGameMaster.IsSequencingEnabled && introTimeline && Director)
                PlayScene(introTimeline);
        }

        public void SkipScene()
        {
            if (Director == null || Director.state != PlayState.Playing) return;
            Director.Stop();
            Debug.Log("Cutscene Skipped!");
        }

        public static void PlayScene(PlayableAsset timeline)
        {
            if (Director == null || timeline == null) return;
            Director.playableAsset = timeline;
            Director.Play();
        }
    }
}