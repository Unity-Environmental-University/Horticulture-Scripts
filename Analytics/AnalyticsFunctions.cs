using Unity.Services.Analytics;
using UnityEngine;

namespace _project.Scripts.Analytics
{
    public class AnalyticsFunctions : MonoBehaviour
    {
        public static void RecordTreatment(string plantName, string affliction, string treatment, bool success)
        {
            var ev = new TreatmentAppliedEvent
            {
                PlantName = plantName,
                TreatmentName = treatment,
                AfflictionName = affliction,
                TreatmentSuccess = success
            };

            AnalyticsService.Instance.RecordEvent(ev);
        }

        public static void RecordRedraw(string discarded, string drawn, int score, int round, int turn)
        {
            if (AnalyticsService.Instance == null) return;

            var ev = new RedrawHandEvent
            {
                CardsDiscarded = discarded,
                CardsDrawn = drawn,
                CurrentScore = score,
                CurrentRound = round,
                CurrentTurn = turn
            };

            AnalyticsService.Instance.RecordEvent(ev);
        }

        public static void RecordAffliction(string plantName, string afflictionName, int round, int turn)
        {
            if (AnalyticsService.Instance == null) return;

            var ev = new AfflictionAppliedEvent
            {
                PlantName = plantName,
                AfflictionName = afflictionName,
                CurrentRound = round,
                CurrentTurn = turn
            };

            AnalyticsService.Instance.RecordEvent(ev);
        }

        public static void RecordRoundStart(int round, int plantsCount, int score, int goal, bool isTutorial)
        {
            if (AnalyticsService.Instance == null) return;

            var ev = new RoundStartEvent
            {
                CurrentRound = round,
                PlantsCount = plantsCount,
                CurrentScore = score,
                MoneyGoal = goal,
                IsTutorial = isTutorial
            };

            AnalyticsService.Instance.RecordEvent(ev);
        }

        public static void RecordRoundEnd(int round, int totalTurns, int finalScore, int scoreGained,
            int plantsHealthy, int plantsDead, bool roundWon)
        {
            if (AnalyticsService.Instance == null) return;

            var ev = new RoundEndEvent
            {
                CurrentRound = round,
                TotalTurns = totalTurns,
                FinalScore = finalScore,
                ScoreGained = scoreGained,
                PlantsHealthy = plantsHealthy,
                PlantsDead = plantsDead,
                RoundWon = roundWon
            };

            AnalyticsService.Instance.RecordEvent(ev);
        }

        public static void RecordTurnStart(int round, int turn, int cardsDrawn, int score, int afflictedPlants)
        {
            if (AnalyticsService.Instance == null) return;

            var ev = new TurnStartEvent
            {
                CurrentRound = round,
                CurrentTurn = turn,
                CardsDrawn = cardsDrawn,
                CurrentScore = score,
                PlantsWithAfflictions = afflictedPlants
            };

            AnalyticsService.Instance.RecordEvent(ev);
        }

        public static void RecordTurnEnd(int round, int turn, int score)
        {
            if (AnalyticsService.Instance == null) return;

            var ev = new TurnEndEvent
            {
                CurrentRound = round,
                CurrentTurn = turn,
                CurrentScore = score
            };

            AnalyticsService.Instance.RecordEvent(ev);
        }
    }
}
