using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Core;
using TMPro;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager Instance { get; set; }
        private static int Score { get; set; }
        private static TextMeshPro TreatmentCostText => CardGameMaster.Instance.treatmentCostText;
        private static TextMeshPro PotentialProfitText => CardGameMaster.Instance.potentialProfitText;
        private List<PlantController> cachedPlants = new();

        public int treatmentCost;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ResetScore()
        {
            Score = 0;
            UpdateScoreText();
        }

        private static void UpdateScoreText()
        {
            if (CardGameMaster.Instance.scoreText)
                CardGameMaster.Instance.scoreText.text = "Moneys: " + "$" + Score;
        }

        private static void UpdateCostText(int totalCost) { TreatmentCostText.text = "Potential Loss: " + totalCost; }
        
        private static void UpdateProfitText(int potProfit) {PotentialProfitText.text = $"Potential Profit: " + potProfit;}

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public int CalculateScore()
        {
            var plantScore = 0;
            var afflictionDamage = 0;

            foreach (var plant in cachedPlants)
            {
                if (plant.PlantCard.Value != null && plant.CurrentAfflictions.Count <= 0)
                    plantScore += plant.PlantCard.Value.Value;

                if (plant.CurrentAfflictions.Any())
                    afflictionDamage += plant.CurrentAfflictions.Select(affliction => affliction.GetCard()!.Value)
                        .Where(damage => damage != null).Sum(damage => damage.Value);
            }

            Debug.Log("Plant Score: " + plantScore);
            Debug.Log("Affliction Score: " + afflictionDamage);
            Debug.Log("Treatment Cost: " + treatmentCost);
            Debug.Log("Current Score: " + Score);
            
            Score += plantScore + afflictionDamage + treatmentCost;

            UpdateScoreText();
            UpdateCostText(0);
            UpdateProfitText(0);
            return Score;
        }

        public int CalculateTreatmentCost()
        {
            var afflictionScore = 0;

            cachedPlants = GetPlatsControllers();

            foreach (var plant in cachedPlants)
            {
                if (plant.PlantCard.Value != null && plant.CurrentAfflictions.Count <= 0) { }

                if (plant.CurrentAfflictions.Any())
                    afflictionScore += plant.CurrentAfflictions.Select(affliction => affliction.GetCard()!.Value)
                        .Where(damage => damage != null).Sum(damage => damage.Value);
            }

            var totalCost = afflictionScore + treatmentCost;

            UpdateCostText(totalCost);

            return totalCost;
        }

        public int CalculatePotentialProfit()
        {
            cachedPlants = GetPlatsControllers();

            var plantScore = cachedPlants
                .Where(plant => plant.PlantCard.Value != null && plant.CurrentAfflictions.Count <= 0)
                .Sum(plant => plant.PlantCard.Value.Value);
            
            UpdateProfitText(plantScore);

            return plantScore;
        }

        private static List<PlantController> GetPlatsControllers()
        {
            var plants = CardGameMaster.Instance.deckManager.plantLocations
                .Select(location => location.GetComponentInChildren<PlantController>(false))
                .Where(controller => controller)
                .ToList();
            
            return plants;
        }
    }
}