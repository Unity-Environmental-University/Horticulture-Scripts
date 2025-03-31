using UnityEngine;

namespace _project.Scripts.Audio
{
    public class AudioVolumeProx : MonoBehaviour
    {
        public AudioSource audioSource;
        public GameObject player;
        public float maxDistance = 100f;

        private float _distance;

        private void Update()
        {
            if (player is null || audioSource is null) return;
            var distance = Vector3.Distance(player.transform.position, transform.position);
            audioSource.volume = CalculateVolume(distance, maxDistance);
        }

        private static float CalculateVolume(float distance, float max)
        {
            var normalizedDistance = Mathf.Clamp(distance, 0, max);
            var volume = Mathf.Max(0, 1 - normalizedDistance / max);
            return volume;
        }
    }
}