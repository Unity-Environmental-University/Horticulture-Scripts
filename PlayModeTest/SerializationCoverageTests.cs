using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using NUnit.Framework;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Validates that all afflictions and treatments in the prototype decks have
    ///     corresponding serialization support in DeckManager for save/load functionality.
    /// </summary>
    /// <remarks>
    ///     This test prevents bugs where a new affliction or treatment is added to the game
    ///     but forgotten in GetAfflictionFromString() or GetTreatmentFromString(), which would
    ///     cause save/load failures where players lose their afflictions/treatments.
    /// </remarks>
    public class SerializationCoverageTests
    {
        private static MethodInfo _getAfflictionFromStringMethod;
        private static MethodInfo _getTreatmentFromStringMethod;
        private static FieldInfo _prototypeAfflictionsDeckField;
        private static FieldInfo _prototypeActionDeckField;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var deckManagerType = typeof(DeckManager);

            // Get reflection access to private methods
            _getAfflictionFromStringMethod = deckManagerType.GetMethod(
                "GetAfflictionFromString",
                BindingFlags.NonPublic | BindingFlags.Static);

            _getTreatmentFromStringMethod = deckManagerType.GetMethod(
                "GetTreatmentFromString",
                BindingFlags.NonPublic | BindingFlags.Static);

            // Get reflection access to prototype decks
            _prototypeAfflictionsDeckField = deckManagerType.GetField(
                "PrototypeAfflictionsDeck",
                BindingFlags.NonPublic | BindingFlags.Static);

            _prototypeActionDeckField = deckManagerType.GetField(
                "PrototypeActionDeck",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(_getAfflictionFromStringMethod,
                "GetAfflictionFromString method not found - DeckManager API may have changed");
            Assert.IsNotNull(_getTreatmentFromStringMethod,
                "GetTreatmentFromString method not found - DeckManager API may have changed");
            Assert.IsNotNull(_prototypeAfflictionsDeckField,
                "PrototypeAfflictionsDeck field not found - DeckManager API may have changed");
            Assert.IsNotNull(_prototypeActionDeckField,
                "PrototypeActionDeck field not found - DeckManager API may have changed");
        }

        [Test]
        public void AllAfflictionsInPrototypeDeck_HaveSerializationSupport()
        {
            // Get the prototype affliction deck
            var prototypeAfflictionsDeck = _prototypeAfflictionsDeckField.GetValue(null) as List<ICard>;
            Assert.IsNotNull(prototypeAfflictionsDeck, "Failed to retrieve PrototypeAfflictionsDeck");
            Assert.IsNotEmpty(prototypeAfflictionsDeck, "PrototypeAfflictionsDeck is empty");

            var missingAfflictions = new List<string>();

            foreach (var card in prototypeAfflictionsDeck)
            {
                if (card is not IAfflictionCard afflictionCard) continue;

                var affliction = afflictionCard.Affliction;
                var afflictionName = affliction.Name;

                // Try to deserialize using GetAfflictionFromString

                if (_getAfflictionFromStringMethod.Invoke(null, new object[] { afflictionName }) is not
                    PlantAfflictions.IAffliction deserializedAffliction) missingAfflictions.Add(afflictionName);
            }

            Assert.IsEmpty(missingAfflictions,
                "The following afflictions are missing from GetAfflictionFromString() in DeckManager:\n" +
                $"  - {string.Join("\n  - ", missingAfflictions)}\n\n" +
                "Add these afflictions to the switch statement in GetAfflictionFromString() to support save/load.");
        }

        [Test]
        public void AllTreatmentsInPrototypeDeck_HaveSerializationSupport()
        {
            // Get the prototype action deck
            var prototypeActionDeck = _prototypeActionDeckField.GetValue(null) as List<ICard>;
            Assert.IsNotNull(prototypeActionDeck, "Failed to retrieve PrototypeActionDeck");
            Assert.IsNotEmpty(prototypeActionDeck, "PrototypeActionDeck is empty");

            var missingTreatments = new List<string>();
            var checkedTreatments = new HashSet<string>(); // Avoid duplicates

            foreach (var treatmentName in from card in prototypeActionDeck
                     where card.Treatment != null
                     select card.Treatment
                     into treatment
                     select treatment.Name
                     into treatmentName
                     where checkedTreatments.Add(treatmentName)
                     select treatmentName)
                // Try to deserialize using GetTreatmentFromString
                if (_getTreatmentFromStringMethod.Invoke(null, new object[] { treatmentName }) is not
                    PlantAfflictions.ITreatment deserializedTreatment)
                    missingTreatments.Add(treatmentName);

            Assert.IsEmpty(missingTreatments,
                "The following treatments are missing from GetTreatmentFromString() in DeckManager:\n" +
                $"  - {string.Join("\n  - ", missingTreatments)}\n\n" +
                "Add these treatments to the switch statement in GetTreatmentFromString() to support save/load.");
        }

        [Test]
        public void GetAfflictionFromString_ReturnsCorrectTypes()
        {
            // Test a few known afflictions return the correct type
            var testCases = new Dictionary<string, Type>
            {
                { "Aphids", typeof(PlantAfflictions.AphidsAffliction) },
                { "Thrips", typeof(PlantAfflictions.ThripsAffliction) },
                { "Mildew", typeof(PlantAfflictions.MildewAffliction) },
                { "Dehydrated", typeof(PlantAfflictions.DehydratedAffliction) }
            };

            foreach (var testCase in testCases)
            {
                var result = _getAfflictionFromStringMethod.Invoke(null, new object[] { testCase.Key })
                    as PlantAfflictions.IAffliction;

                Assert.IsNotNull(result,
                    $"GetAfflictionFromString(\"{testCase.Key}\") returned null");
                Assert.AreEqual(testCase.Value, result.GetType(),
                    $"GetAfflictionFromString(\"{testCase.Key}\") returned wrong type");
            }
        }

        [Test]
        public void GetTreatmentFromString_ReturnsCorrectTypes()
        {
            // Test a few known treatments return the correct type
            var testCases = new Dictionary<string, Type>
            {
                { "Horticultural Oil", typeof(PlantAfflictions.HorticulturalOilTreatment) },
                { "Fungicide", typeof(PlantAfflictions.FungicideTreatment) },
                { "Panacea", typeof(PlantAfflictions.Panacea) }
            };

            foreach (var testCase in testCases)
            {
                var result = _getTreatmentFromStringMethod.Invoke(null, new object[] { testCase.Key })
                    as PlantAfflictions.ITreatment;

                Assert.IsNotNull(result,
                    $"GetTreatmentFromString(\"{testCase.Key}\") returned null");
                Assert.AreEqual(testCase.Value, result.GetType(),
                    $"GetTreatmentFromString(\"{testCase.Key}\") returned wrong type");
            }
        }

        [Test]
        public void GetAfflictionFromString_InvalidName_ReturnsNull()
        {
            var result = _getAfflictionFromStringMethod.Invoke(null, new object[] { "NonExistentAffliction" })
                as PlantAfflictions.IAffliction;

            Assert.IsNull(result,
                "GetAfflictionFromString should return null for invalid affliction names");
        }

        [Test]
        public void GetTreatmentFromString_InvalidName_ReturnsNull()
        {
            var result = _getTreatmentFromStringMethod.Invoke(null, new object[] { "NonExistentTreatment" })
                as PlantAfflictions.ITreatment;

            Assert.IsNull(result,
                "GetTreatmentFromString should return null for invalid treatment names");
        }
    }
}