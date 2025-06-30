using System.Collections;
using UnityEngine;

namespace _project.Scripts.Core
{
    public class RobotVoiceController : MonoBehaviour
    {
        [Header("Audio Source")]
        [SerializeField] private AudioSource robotAudioSource;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip introAudio;

        private void PlayAudio(AudioClip clip)
        {
            robotAudioSource.pitch = 1f;
            robotAudioSource.volume = 1f;
            robotAudioSource.spatialBlend = 1f;

            robotAudioSource.minDistance = 1f;
            robotAudioSource.maxDistance = 50f;

            robotAudioSource.PlayOneShot(clip);
        }

        public IEnumerator PlayFailAudio()
        {
            PlayAudio(introAudio);
            yield return new WaitForSeconds(introAudio.length);
        }
    }
}
