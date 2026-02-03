using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Core;
using TMPro;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class IBonus
    {
        public string Name { get; set; }
        public int BonusValue { get; set; }
    }

    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager Instance { get; set; }
        private const int StartingMoneys = 10;
        private static int Moneys { get; set; }
        private static TextMeshPro TreatmentCostText => CardGameMaster.Instance?.treatmentCostText;
        private static TextMeshPro PotentialProfitText => CardGameMaster.Instance?.potentialProfitText;
        private List<PlantController> cachedPlants = new();

        public List<IBonus> bonuses = new();
        public int treatmentCost;
        public bool debugging;

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

        public static void UpdateMoneysText(int modifier = 0)
        {
            if (CardGameMaster.Instance.moneysText)
            {
                var turnController = CardGameMaster.Instance.turnController;
                var displayMoney = Moneys + modifier;

                // Display different format based on game mode
                if (turnController.currentGameMode == GameMode.Campaign)
                {
                    CardGameMaster.Instance.moneysText!.text =
                        $"Money: ${displayMoney} Rent Due: ${turnController.moneyGoal}";
                }
                else
                {
                    // Tutorial or Endless mode - original format
                    CardGameMaster.Instance.moneysText!.text =
                        "Moneys: " + "$" + displayMoney + "/" + turnController.moneyGoal;
                }
            }

            if (CardGameMaster.Instance.shopMoneyText)
                CardGameMaster.Instance.shopMoneyText!.text = "Moneys: " + "$" + (Moneys + modifier);
        }

        public static void SetScore(int newScore)
        {
            Moneys = newScore;
            UpdateMoneysText();
        } 

        private static void UpdateCostText(int totalCost)
        {
            var text = TreatmentCostText;
            if (text) text.text = "Potential Loss: " + totalCost;
        }
        
        private static void UpdateProfitText(int potProfit)
        {
            var text = PotentialProfitText;
            if (text) text.text = "Potential Profit: " + potProfit;
        }

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
            
            var bonusValue = CalculateBonuses();

            if (debugging)
            {
                Debug.Log("Plant Value: " + plantValue);
                Debug.Log("Affliction Damage: " + afflictionDamage);
                Debug.Log("Treatment Cost: " + treatmentCost);
                Debug.Log("Current Moneys: " + Moneys);
                foreach (var bonus in bonuses) Debug.Log("Bonus Applied: " + bonus.Name + ": " + bonus.BonusValue);
            }

            Moneys += plantValue + afflictionDamage + treatmentCost + bonusValue;

            UpdateMoneysText();
            UpdateCostText(0);
            UpdateProfitText(0);
            bonuses.Clear();
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

        public static void SubtractMoneys(int amount)
        {
            Moneys -= amount;
            UpdateMoneysText();
        }

        private static List<PlantController> GetPlatsControllers()
        {
            var gameMaster = CardGameMaster.Instance;
            if (!gameMaster || !gameMaster.deckManager || gameMaster.deckManager.plantLocations == null)
                return new List<PlantController>();

            var plants = new List<PlantController>(gameMaster.deckManager.plantLocations.Count);

            foreach (var location in gameMaster.deckManager.plantLocations)
            {
                if (!location) continue;

                var plantTransform = location.Transform;
                if (!plantTransform) continue;

                var controller = plantTransform.GetComponentInChildren<PlantController>(false);
                if (controller)
                    plants.Add(controller);
            }

            return plants;
        }

        private int CalculateBonuses()
        {
            var totalBonus = bonuses.Sum(b => b.BonusValue);
            return totalBonus;
        }
    }
}
