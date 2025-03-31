using UnityEngine;

namespace _project.Scripts.Handlers
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicBounce : MonoBehaviour
    {
        public float scaleMultiplier = 1.0f;
        public float smoothTime = 0.2f;
        public float maxScale = 2.0f;
        public float minScale = 1.0f;

        [SerializeField] private AudioSource audioSource;
        private readonly float[] _spectrumData = new float[64];
        private Vector3 _originalScale;
        private Vector3 _targetScale;

        private void Start()
        {
            if (!audioSource) audioSource = GetComponent<AudioSource>();
            _originalScale = transform.localScale;
        }

        private void Update()
        {
            audioSource.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris);

            var intensity = 0f;
            for (var i = 0; i < 10; i++) // first 10 for performance 
                intensity += _spectrumData[i];

            var scale = Mathf.Clamp(minScale + intensity * scaleMultiplier, minScale, maxScale);
            _targetScale = _originalScale * scale;
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, smoothTime);
        }
    }
}