using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Classes
{
    public class PlantEffectRequest
    {
        public readonly PlantController Plant;
        public readonly ParticleSystem Particle;
        public readonly AudioClip Sound;
        public readonly float Delay;

        public PlantEffectRequest(PlantController plant, ParticleSystem particle, AudioClip sound, float delay)
        {
            Plant = plant;
            Particle = particle;
            Sound = sound;
            Delay = delay;
        }
    }
}