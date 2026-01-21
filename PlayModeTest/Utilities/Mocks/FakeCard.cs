using System.Collections.Generic;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Stickers;
using UnityEngine;

namespace _project.Scripts.PlayModeTest.Utilities.Mocks
{
    /// <summary>
    ///     Unified fake card implementation for testing.
    ///     Consolidates duplicate FakeCard implementations from DeckTester, SideDeckTests, and TurnTester.
    /// </summary>
    public class FakeCard : ICard
    {
        /// <summary>
        ///     Basic constructor for simple card tests (DeckTester, SideDeckTests pattern).
        /// </summary>
        public FakeCard(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Extended constructor for treatment tests (TurnTester pattern).
        /// </summary>
        public FakeCard(string name, PlantAfflictions.ITreatment treatment) : this(name)
        {
            Treatment = treatment;
        }

        /// <summary>
        ///     Full constructor for complete card configuration.
        /// </summary>
        public FakeCard(string name, PlantAfflictions.ITreatment treatment, int? value) : this(name, treatment)
        {
            Value = value;
        }

        public string Name { get; }
        public string Description { get; set; } = "Test card";
        public PlantAfflictions.ITreatment Treatment { get; set; }
        public Material Material { get; set; }
        public List<ISticker> Stickers { get; } = new();

        public int? Value { get; set; }

        public GameObject Prefab => CardGameMaster.Instance?.actionCardPrefab;

        public ICard Clone()
        {
            var clone = new FakeCard(Name, Treatment, Value)
            {
                Description = Description,
                Material = Material
            };
            foreach (var sticker in Stickers) clone.Stickers.Add(sticker.Clone());
            return clone;
        }
    }
}