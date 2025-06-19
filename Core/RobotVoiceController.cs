using System.Collections;
using UnityEngine;

namespace _project.Scripts.Core
{
    public class RobotVoiceController : MonoBehaviour
    {
        [SerializeField] private AudioSource introAudio;

        private static void PlayAudio(AudioSource clip) { clip.Play(); }

        public IEnumerator PlayIntroAudio()
        {
            PlayAudio(introAudio);
            yield return new WaitForSeconds(introAudio.clip.length);
        }

        public IEnumerator PlayFailAudio()
        {
            PlayAudio(introAudio);
            yield return new WaitForSeconds(introAudio.clip.length);
        }
    }
}
