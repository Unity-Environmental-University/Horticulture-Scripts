using UnityEngine;
using UnityEngine.Audio;

namespace _project.Scripts.Audio
{
    public class AudioSwitchCollider : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            mixer.SetFloat("IndoorVolume", 0);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            mixer.SetFloat("IndoorVolume", -20);
        }
    }
}