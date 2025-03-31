using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _project.Scripts.Audio
{
    public class AudioSyncer : MonoBehaviour
    {
        public List<AudioSource> audioSources = new();

        public float syncDelay = 0.1f;

        private void Start()
        {
            if (!CheckAllSourcesHaveSameClip())
                Debug.LogError("Audio Syncer: Not all audio sources have the same clip!");
            else
                PlaySync();
        }

        private void PlaySync()
        {
            var dspTime = AudioSettings.dspTime;

            foreach (var source in audioSources) source.PlayScheduled(dspTime + syncDelay);
        }

        private bool CheckAllSourcesHaveSameClip()
        {
            if (audioSources.Count == 0) return true;

            var refClip = audioSources[0].clip;

            return audioSources.All(source => source.clip == refClip);
        }
    }
}