using _project.Scripts.Card_Core;
using UnityEngine;
using UnityEngine.Playables;

namespace _project.Scripts.Cinematics
{
    public class CinematicDirector : MonoBehaviour
    {
        public static PlayableDirector director;
        public PlayableAsset introTimeline;
        public PlayableAsset aphidsTimeline;
        public PlayableAsset postAphidsTimeline;

        private void Awake()
        {
            if (director == null)
                director = FindFirstObjectByType<PlayableDirector>();
            var gm = CardGameMaster.Instance;
            if (gm?.turnController != null)
                gm.turnController.ReadyToPlay = () => director != null && director.state != PlayState.Playing;
        }

        private void Start()
        {
            if (CardGameMaster.IsSequencingEnabled && introTimeline && director)
                PlayScene(introTimeline);
        }

        public void SkipScene()
        {
            if (director == null || director.state != PlayState.Playing) return;
            director.Stop();
            Debug.Log("Cutscene Skipped!");
        }

        public static void PlayScene(PlayableAsset timeline)
        {
            if (director == null || timeline == null) return;
            director.playableAsset = timeline;
            director.Play();
        }
    }
}