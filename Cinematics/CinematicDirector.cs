using UnityEngine;
using UnityEngine.Playables;

namespace _project.Scripts.Cinematics
{
    public class CinematicDirector : MonoBehaviour
    {
        [SerializeField] private PlayableDirector director;

        private void Start()
        {
            if (director == null) director = FindFirstObjectByType<PlayableDirector>();
            director.Play();
        }
    }
}