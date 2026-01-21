using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using UnityEngine;

namespace _project.Scripts.PlayModeTest.Utilities.Mocks
{
    /// <summary>
    ///     Fake affliction for testing plant treatment application.
    /// </summary>
    public class FakeAffliction : PlantAfflictions.IAffliction
    {
        private static readonly List<PlantAfflictions.ITreatment> DefaultTreatments = new()
        {
            new FakeTreatment()
        };

        public FakeAffliction()
        {
            AcceptableTreatments = DefaultTreatments;
        }

        public FakeAffliction(List<PlantAfflictions.ITreatment> treatments)
        {
            AcceptableTreatments = treatments ?? DefaultTreatments;
        }

        public string Name { get; set; } = "Test Affliction";
        public string Description { get; set; } = "Just a test";
        public Color Color { get; set; } = Color.gray;
        public Shader Shader => null;

        public List<PlantAfflictions.ITreatment> AcceptableTreatments { get; }

        public PlantAfflictions.IAffliction Clone()
        {
            return new FakeAffliction(AcceptableTreatments)
            {
                Name = Name,
                Description = Description,
                Color = Color
            };
        }

        public bool CanBeTreatedBy(PlantAfflictions.ITreatment treatment)
        {
            return AcceptableTreatments.Any(t => t.GetType() == treatment.GetType());
        }

        public bool TreatWith(PlantAfflictions.ITreatment treatment, PlantController plant)
        {
            if (!CanBeTreatedBy(treatment)) return false;
            plant.RemoveAffliction(this);
            return true;
        }

        public void TickDay(PlantController plant)
        {
            // No-op for testing
        }
    }
}