using UnityEngine;

namespace _project.Scripts.Audio
{
    public class RobotAudioManager : MonoBehaviour
    {
        private AudioSource _audioSource;

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void PlayAudio(AudioClip clip)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
}
