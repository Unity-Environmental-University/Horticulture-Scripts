using UnityEngine;

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
        public AudioClip placeCard;
        public AudioClip unplaceCard;
        public AudioClip shuffleCard;

        [Header("Affliction Sounds")]
        public AudioClip thripsAfflicted;
        public AudioClip aphidsAfflicted;
        public AudioClip mealyBugsAfflicted;
        public AudioClip mildewAfflicted;
    }
}