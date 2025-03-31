using System.Linq;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager Instance { get; set; }
        private static int Score { get; set; }
        private static int BeeScore { get; set; }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public int CalculateScore()
        {
            var plantScore = 0;
            var afflictionScore = 0;
            BeeScore = 10;

            var plants = CardGameMaster.Instance.deckManager.plantLocations
                .Select(location => location.GetComponentInChildren<PlantController>(false))
                .Where(controller => controller)
                .ToList();

            foreach (var plant in plants)
            {
                if (plant.PlantCard.Value != null) plantScore += plant.PlantCard.Value.Value;

                afflictionScore += plant.CurrentAfflictions.Sum(affliction => affliction.Damage);

                BeeScore += plant.UsedTreatments.Sum(treatment => treatment.BeeValue);
            }

            Debug.Log("Score: " + Score + " / " + plantScore + " / " + afflictionScore + " / " + BeeScore);
            
            Score = (plantScore - afflictionScore) * BeeScore;

            if (CardGameMaster.Instance.scoreText)
                CardGameMaster.Instance.scoreText.text = "Score: " + Score;
            return Score;
        }
    }
}