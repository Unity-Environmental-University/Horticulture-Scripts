using System.Linq;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager Instance { get; set; }
        private static int Score { get; set; }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public int CalculateScore()
        {
            var plantScore = 0;
            var afflictionScore = 0;

            var plants = CardGameMaster.Instance.deckManager.plantLocations
                .Select(location => location.GetComponentInChildren<PlantController>(false))
                .Where(controller => controller)
                .ToList();

            foreach (var plant in plants)
            {
                if (plant.PlantCard.Value != null && plant.CurrentAfflictions.Count < 0)
                    plantScore += plant.PlantCard.Value.Value;

                if (plant.CurrentAfflictions.Any())
                    afflictionScore += plant.CurrentAfflictions.Select(affliction => affliction.GetCard()!.Value)
                        .Where(damage => damage != null).Sum(damage => damage.Value);
            }

            Debug.Log("Plant Score: " + plantScore);
            Debug.Log("Affliction Score: " + afflictionScore);
            Debug.Log("Current Score: " + Score);

            Score += plantScore + afflictionScore;

            if (CardGameMaster.Instance.scoreText)
                CardGameMaster.Instance.scoreText.text = "Score: " + Score;
            return Score;
        }
    }
}