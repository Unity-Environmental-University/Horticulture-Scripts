using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Classes
{
    public class PlantEffectRequest
    {
        public readonly PlantController plant;
        public readonly ParticleSystem particle;
        public readonly AudioClip sound;
        public readonly float delay;

        public PlantEffectRequest(PlantController plant, ParticleSystem particle, AudioClip sound, float delay)
        {
            this.plant = plant;
            this.particle = particle;
            this.sound = sound;
            this.delay = delay;
        }
    }
}