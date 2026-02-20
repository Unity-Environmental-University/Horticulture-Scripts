using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Core;
using DG.Tweening;
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

        public readonly List<IBonus> bonuses = new();
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
            var displayMoney = Moneys + modifier;
            var turnController = CardGameMaster.Instance ? CardGameMaster.Instance.turnController : null;

            if (CardGameMaster.Instance.moneysText && turnController)
                CardGameMaster.Instance.moneysText!.text = FormatMainMoneyText(displayMoney, turnController);

            if (CardGameMaster.Instance.shopMoneyText)
                CardGameMaster.Instance.shopMoneyText!.text = FormatShopMoneyText(displayMoney);
        }

        private static string FormatMainMoneyText(int value, TurnController tc)
        {
            return tc.currentGameMode == GameMode.Campaign
                ? $"Money: ${value} Rent Due: ${tc.moneyGoal}"
                : $"Moneys: ${value}/{tc.moneyGoal}";
        }

        private static string FormatShopMoneyText(int value)
        {
            return $"Moneys: ${value}";
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

        #region Score Animation

        private static readonly Color GainColor = new(0.2f, 0.9f, 0.3f);
        private static readonly Color LossColor = new(0.95f, 0.25f, 0.2f);
        private const float CountDuration = 1.0f;
        private const float PunchScale = 0.35f;
        private const float PunchDuration = 1.5f;

        private Sequence _moneyAnimSequence;

        private void SafeKillSequence()
        {
            if (_moneyAnimSequence != null && _moneyAnimSequence.IsActive())
                _moneyAnimSequence.Kill();
            _moneyAnimSequence = null;
        }

        private void OnDestroy() => SafeKillSequence();

        /// <summary>
        ///     Animates the money display from <paramref name="previousScore" /> to <paramref name="finalScore" />
        ///     with a counting tween, color flash, and scale punch.
        ///     Call after <see cref="CalculateScore" /> which sets the canonical value.
        /// </summary>
        public IEnumerator AnimateScoreChange(int previousScore, int finalScore)
        {
            var delta = finalScore - previousScore;
            if (delta == 0) yield break;

            SafeKillSequence();

            var moneysText = CardGameMaster.Instance ? CardGameMaster.Instance.moneysText : null;
            var shopText = CardGameMaster.Instance ? CardGameMaster.Instance.shopMoneyText : null;

            if (!moneysText && !shopText) yield break;

            var flashColor = delta > 0 ? GainColor : LossColor;
            var displayValue = previousScore;

            // Reset display to old value — CalculateScore already set the final value
            SetMoneyTextRaw(previousScore);

            var seq = DOTween.Sequence();
            var origMainColor = moneysText ? moneysText.color : Color.white;
            var origShopColor = shopText ? shopText.color : Color.white;

            // Set flash color immediately
            if (moneysText) moneysText.color = flashColor;
            if (shopText) shopText.color = flashColor;

            // Count tween: animate displayValue from previous to final
            seq.Append(
                DOTween.To(() => displayValue, x =>
                    {
                        displayValue = x;
                        SetMoneyTextRaw(x);
                    }, finalScore, CountDuration)
                    .SetEase(Ease.OutQuart)
            );

            // Scale punch runs in parallel with count
            if (moneysText)
                seq.Join(moneysText.transform.DOPunchScale(Vector3.one * PunchScale, PunchDuration, 6, 0.7f));
            if (shopText)
                seq.Join(shopText.transform.DOPunchScale(Vector3.one * PunchScale, PunchDuration, 6, 0.7f));

            // Color fades back to the original over the count duration
            if (moneysText)
                seq.Join(
                    DOTween.To(() => moneysText.color, c => moneysText.color = c, origMainColor, CountDuration)
                        .SetEase(Ease.InQuad)
                );
            if (shopText)
                seq.Join(
                    DOTween.To(() => shopText.color, c => shopText.color = c, origShopColor, CountDuration)
                        .SetEase(Ease.InQuad)
                );

            // Ensure canonical final state
            var complete = false;
            seq.OnComplete(() =>
            {
                if (moneysText) moneysText.color = origMainColor;
                if (shopText) shopText.color = origShopColor;
                UpdateMoneysText();
                complete = true;
            });

            seq.SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _moneyAnimSequence = seq;

            // Wait for the sequence to actually finish (robust against timing changes)
            yield return new WaitUntil(() => complete || _moneyAnimSequence == null);
        }

        private static void SetMoneyTextRaw(int displayValue)
        {
            var moneysText = CardGameMaster.Instance ? CardGameMaster.Instance.moneysText : null;
            var shopText = CardGameMaster.Instance ? CardGameMaster.Instance.shopMoneyText : null;
            var turnController = CardGameMaster.Instance ? CardGameMaster.Instance.turnController : null;

            if (moneysText && turnController)
                moneysText.text = FormatMainMoneyText(displayValue, turnController);

            if (shopText)
                shopText.text = FormatShopMoneyText(displayValue);
        }

        #endregion
    }
}
