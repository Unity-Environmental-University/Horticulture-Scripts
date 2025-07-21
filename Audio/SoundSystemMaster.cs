using _project.Scripts.Classes;
using UnityEngine;

// ReSharper disable UnusedMember.Global

namespace _project.Scripts.Audio
{
    public class SoundSystemMaster : MonoBehaviour
    {
        [Header("Plant Sounds")]
        public AudioClip plantSpawn;
        public AudioClip plantHeal;
        public AudioClip plantSell;
        public AudioClip plantDeath;

        [Header("Card Sounds")]
        public AudioClip selectCard;
        public AudioClip drawCard;
        public AudioClip placeCard;
        public AudioClip unplaceCard;
        public AudioClip shuffleCard;

        [Header("Affliction Sounds")]
        public AudioClip thripsAfflicted;
        public AudioClip aphidsAfflicted;
        public AudioClip mealyBugsAfflicted;
        public AudioClip mildewAfflicted;
        
        [Header("Narration Clips")]
        public AudioClip florabotNarrationAphids;

        public AudioClip GetInsectSound(PlantAfflictions.IAffliction affliction)
        {
            return affliction switch
            {
                PlantAfflictions.AphidsAffliction => aphidsAfflicted,
                PlantAfflictions.ThripsAffliction => thripsAfflicted,
                PlantAfflictions.MealyBugsAffliction => mealyBugsAfflicted,
                _ => null
            };
        }
    }
}