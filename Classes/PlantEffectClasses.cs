using _project.Scripts.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace _project.Scripts.Classes
{
    public class PlantEffectRequest
    {
        public PlantController Plant;
        public ParticleSystem Particle;
        public AudioResource Sound;
        public float Delay;

        public PlantEffectRequest(PlantController plant, ParticleSystem particle, AudioResource sound, float delay)
        {
            Plant = plant;
            Particle = particle;
            Sound = sound;
            Delay = delay;
        }
    }
}