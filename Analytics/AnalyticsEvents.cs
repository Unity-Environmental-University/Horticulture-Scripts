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

        public string CardsDrawn { set => SetParameter("cardsDrawn", value); }
        public string CardsDiscarded { set => SetParameter("cardsDiscarded", value); }
        public int CurrentScore { set => SetParameter("currentScore", value); }
        public int CurrentRound { set => SetParameter("currentRound", value); }
        public int CurrentTurn { set => SetParameter("currentTurn", value); }
        public bool Success { set => SetParameter("success", value); }
        public string BlockReason { set => SetParameter("blockReason", value); }
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

    /// <summary>
    /// Analytics event fired when a game round ends, capturing comprehensive round performance metrics.
    /// </summary>
    /// <remarks>
    /// This event distinguishes between "winning" a round (positive score delta) and achieving
    /// "victory" (meeting the actual win condition). This distinction is critical for understanding
    /// player progression and success rates.
    /// </remarks>
    public class RoundEndEvent : Event
    {
        public RoundEndEvent() : base("round_end") { }

        /// <summary>
        /// Gets or sets the current round number.
        /// </summary>
        public int CurrentRound { set => SetParameter("currentRound", value); }

        /// <summary>
        /// Gets or sets the total number of turns taken in this round.
        /// </summary>
        public int TotalTurns { set => SetParameter("totalTurns", value); }

        /// <summary>
        /// Gets or sets the player's final score (money) at the end of the round.
        /// </summary>
        public int FinalScore { set => SetParameter("finalScore", value); }

        /// <summary>
        /// Gets or sets the change in score during this round (can be positive or negative).
        /// </summary>
        /// <remarks>
        /// A positive value indicates profit, while a negative value indicates loss.
        /// </remarks>
        public int ScoreDelta { set => SetParameter("scoreDelta", value); }

        /// <summary>
        /// Gets or sets the count of plants with no afflictions at round end.
        /// </summary>
        public int PlantsHealthy { set => SetParameter("plantsHealthy", value); }

        /// <summary>
        /// Gets or sets the count of plants that died during the round.
        /// </summary>
        public int PlantsDead { set => SetParameter("plantsDead", value); }

        /// <summary>
        /// Gets or sets whether the round was "won" based on score improvement (score delta > 0).
        /// </summary>
        /// <remarks>
        /// This is a performance metric indicating the player made profit this round,
        /// but does NOT necessarily mean they achieved the victory condition.
        /// Compare with <see cref="RoundVictory"/> for actual win condition achievement.
        /// </remarks>
        public bool RoundWon { set => SetParameter("roundWon", value); }

        /// <summary>
        /// Gets or sets whether the player achieved actual victory this round by reaching the money goal.
        /// </summary>
        /// <remarks>
        /// This is true ONLY when:
        /// <list type="number">
        /// <item><description>The player's money >= money goal</description></item>
        /// <item><description>The game is NOT in tutorial mode (tutorial steps don't count as victories)</description></item>
        /// </list>
        /// <para>
        /// This metric distinguishes actual game completion from mere round profitability.
        /// A round can be "won" (positive score) without achieving "victory" (reaching goal).
        /// </para>
        /// </remarks>
        /// <seealso cref="RoundWon"/>
        public bool RoundVictory { set => SetParameter("roundVictory", value); }
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
