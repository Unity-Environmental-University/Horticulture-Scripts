namespace _project.Scripts.Classes
{
    /// <summary>
    /// Default no-op treatment so JSON-only mod cards are selectable and playable
    /// without requiring any gameplay fields. Causes no effect when applied.
    /// </summary>
    public class NoopTreatment : PlantAfflictions.ITreatment
    {
        public string Name => "Custom Card";
        public string Description => "No effect";
        public int? InfectCureValue { get; set; } = 0;
        public int? EggCureValue { get; set; } = 0;
    }
}

