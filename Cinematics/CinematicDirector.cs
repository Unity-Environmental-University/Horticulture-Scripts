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

        private void Awake()
        {
            if (director == null)
                director = FindFirstObjectByType<PlayableDirector>();
            CardGameMaster.Instance.turnController.ReadyToPlay = () => director.state != PlayState.Playing;
        }

        private void Start()
        {
            if (CardGameMaster.IsSequencingEnabled)
                PlayScene(introTimeline);
        }

        public void SkipScene()
        {
            if (director.state != PlayState.Playing) return;
            director.Stop();
            Debug.Log("Cutscene Skipped!");
        }

        public static void PlayScene(PlayableAsset timeline)
        {
            director.playableAsset = timeline;
            director.Play();
        }
    }
}