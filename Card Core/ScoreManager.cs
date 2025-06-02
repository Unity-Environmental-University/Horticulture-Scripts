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
        private const int StartingMoneys = 10;
        private static int Moneys { get; set; }
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

        public void ResetMoneys()
        {
            Moneys = StartingMoneys;
            UpdateMoneysText();
        }

        private static void UpdateMoneysText(int modifier = 0)
        {
            if (CardGameMaster.Instance.moneysText)
                CardGameMaster.Instance.moneysText.text = "Moneys: " + "$" + (Moneys + modifier) 
                                                          + "/" + CardGameMaster.Instance.turnController.moneyGoal;
        }

        private static void UpdateCostText(int totalCost) { TreatmentCostText.text = "Potential Loss: " + totalCost; }
        
        private static void UpdateProfitText(int potProfit) {PotentialProfitText.text = "Potential Profit: " + potProfit;}

        // ReSharper disable once MemberCanBeMadeStatic.Global
        public int CalculateScore()
        {
            var plantValue = 0;
            var afflictionDamage = 0;

            foreach (var plant in cachedPlants)
            {
                if (plant.PlantCard.Value != null && plant.CurrentAfflictions.Count <= 0)
                    plantValue += plant.PlantCard.Value.Value;

                if (plant.CurrentAfflictions.Any())
                    afflictionDamage += plant.CurrentAfflictions.Select(affliction => affliction.GetCard()!.Value)
                        .Where(damage => damage != null).Sum(damage => damage.Value);
            }

            Debug.Log("Plant Value: " + plantValue);
            Debug.Log("Affliction Damage: " + afflictionDamage);
            Debug.Log("Treatment Cost: " + treatmentCost);
            Debug.Log("Current Moneys: " + Moneys);
            
            Moneys += plantValue + afflictionDamage + treatmentCost;

            UpdateMoneysText();
            UpdateCostText(0);
            UpdateProfitText(0);
            return Moneys;
        }
        
        public static int GetMoneys(){return Moneys;}

        public void CalculateTreatmentCost()
        {
            var afflictionDamage = 0;

            cachedPlants = GetPlatsControllers();

            foreach (var plant in cachedPlants)
            {
                if (plant.PlantCard.Value != null && plant.CurrentAfflictions.Count <= 0) { }

                if (plant.CurrentAfflictions.Any())
                    afflictionDamage += plant.CurrentAfflictions.Select(affliction => affliction.GetCard()!.Value)
                        .Where(damage => damage != null).Sum(damage => damage.Value);
            }

            UpdateCostText(afflictionDamage);
            UpdateMoneysText(treatmentCost);
        }

        public void CalculatePotentialProfit()
        {
            cachedPlants = GetPlatsControllers();

            var plantValue = cachedPlants
                .Where(plant => plant.PlantCard?.Value != null)
                .Sum(plant => plant.PlantCard.Value.Value);

            UpdateProfitText(plantValue);
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