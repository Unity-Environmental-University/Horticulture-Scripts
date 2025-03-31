using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace _project.Scripts.Handlers
{
    public class IndoorAudioCollider : MonoBehaviour
    {
        [SerializeField] private GameObject player;
        [SerializeField] private AudioMixerGroup outdoorSounds;
        public float smoothTime = 0.5f;
        public float indoorVolume = -5f;

        [Range(0, 1)] private readonly float _volumeTransitionSpeed;
        private float _currentVolume;
        private float _targetVolume;
        private Coroutine _volumeCoroutine;

        public IndoorAudioCollider(float volumeTransitionSpeed)
        {
            _volumeTransitionSpeed = Mathf.Clamp01(volumeTransitionSpeed);
            if (_volumeTransitionSpeed == 0f) Debug.LogError("volumeTransitionSpeed must be between 0 and 1.");

            // Initialize the clamped value
            _volumeTransitionSpeed = Mathf.Clamp01(volumeTransitionSpeed);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _targetVolume = Mathf.Lerp(indoorVolume, 0f, Mathf.Clamp01(_volumeTransitionSpeed));
            StartVolumeTransition();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _targetVolume = 0;
            StartVolumeTransition();
        }

        private void StartVolumeTransition()
        {
            if (_volumeCoroutine != null) StopCoroutine(_volumeCoroutine);
            _volumeCoroutine = StartCoroutine(SmoothTransition());
        }

        private IEnumerator SmoothTransition()
        {
            outdoorSounds.audioMixer.GetFloat("Volume", out _currentVolume);
            var time = 0f;
            while (time < smoothTime)
            {
                time += Time.deltaTime;
                _currentVolume = Mathf.Lerp(_currentVolume, _targetVolume, time / smoothTime);
                outdoorSounds.audioMixer.SetFloat("Volume", _currentVolume);
                yield return null;
            }

            outdoorSounds.audioMixer.SetFloat("Volume", _targetVolume);
        }
    }
}