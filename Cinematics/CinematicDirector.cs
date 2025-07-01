using _project.Scripts.Card_Core;
using UnityEngine;
using UnityEngine.Playables;

namespace _project.Scripts.Cinematics
{
    public class CinematicDirector : MonoBehaviour
    {
        public static PlayableDirector director;

        private void Awake()
        {
            if (director == null)
                director = FindFirstObjectByType<PlayableDirector>();
            CardGameMaster.Instance.turnController.ReadyToPlay = () => director.state != PlayState.Playing;
        }

        private void Start()
        {
            if (CardGameMaster.Instance.isSequencingEnabled)
                director.Play();
        }
    }
}