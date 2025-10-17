using Unity.Services.Analytics;

namespace _project.Scripts.Analytics
{
    public class TreatmentAppliedEvent : Event
    {
        public TreatmentAppliedEvent() : base("treatment_applied") { }

        public string PlantName { set => SetParameter("plantName", value); }
        public string TreatmentName { set => SetParameter("treatmentName", value); }
        public string AfflictionName { set => SetParameter("afflictionName", value); }
        public bool TreatmentSuccess { set => SetParameter("treatmentSuccess", value); }
    }

    public class AfflictionAppliedEvent : Event
    {
        public AfflictionAppliedEvent() : base("affliction_applied") { }

        public string PlantName { set => SetParameter("plantName", value); }
        public string AfflictionName { set => SetParameter("afflictionName", value); }
        public int CurrentRound { set => SetParameter("currentRound", value); }
        public int CurrentTurn { set => SetParameter("currentTurn", value); }
    }

    public class RedrawHandEvent : Event
    {
        public RedrawHandEvent() : base("redraw_hand") { }

        public string CardsDrawn{ set => SetParameter("cardsDrawn", value); }
        public string CardsDiscarded { set => SetParameter("cardsDiscarded", value); }
        public int CurrentScore { set => SetParameter("currentScore", value); }
        public int CurrentRound { set => SetParameter("currentRound", value); }
        public int CurrentTurn { set => SetParameter("currentTurn", value); }
    }

    public class RoundStartEvent : Event
    {
        public RoundStartEvent() : base("round_start") { }

        public int CurrentRound { set => SetParameter("currentRound", value); }
        public int PlantsCount { set => SetParameter("plantsCount", value); }
        public int CurrentScore { set => SetParameter("currentScore", value); }
        public int MoneyGoal { set => SetParameter("moneyGoal", value); }
        public bool IsTutorial { set => SetParameter("isTutorial", value); }
    }

    public class RoundEndEvent : Event
    {
        public RoundEndEvent() : base("round_end") { }

        public int CurrentRound { set => SetParameter("currentRound", value); }
        public int TotalTurns { set => SetParameter("totalTurns", value); }
        public int FinalScore { set => SetParameter("finalScore", value); }
        public int ScoreGained { set => SetParameter("scoreGained", value); }
        public int PlantsHealthy { set => SetParameter("plantsHealthy", value); }
        public int PlantsDead { set => SetParameter("plantsDead", value); }
        public bool RoundWon { set => SetParameter("roundWon", value); }
    }

    public class TurnStartEvent : Event
    {
        public TurnStartEvent() : base("turn_start") { }

        public int CurrentRound { set => SetParameter("currentRound", value); }
        public int CurrentTurn { set => SetParameter("currentTurn", value); }
        public int CardsDrawn { set => SetParameter("cardsDrawn", value); }
        public int CurrentScore { set => SetParameter("currentScore", value); }
        public int PlantsWithAfflictions { set => SetParameter("plantsWithAfflictions", value); }
    }

    public class TurnEndEvent : Event
    {
        public TurnEndEvent() : base("turn_end") { }

        public int CurrentRound { set => SetParameter("currentRound", value); }
        public int CurrentTurn { set => SetParameter("currentTurn", value); }
        public int CurrentScore { set => SetParameter("currentScore", value); }
    }
}
