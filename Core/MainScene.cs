using UnityEngine;

namespace _project.Scripts.Core
{
    public class MainScene : MonoBehaviour
    {
        [SerializeField] private float startingAudioLevel;

        private void Start()
        {
            AudioListener.volume = startingAudioLevel;
        }
    }
}