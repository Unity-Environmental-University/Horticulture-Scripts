using _project.Scripts.Card_Core;
using _project.Scripts.ModLoading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    ///     Simple tests for the simplified ModLoader
    /// </summary>
    public class ModLoaderTest
    {
        private CardGameMaster testMaster;
        private GameObject testObject;

        [SetUp]
        public void SetUp()
        {
            // Expect warning from missing components in test environment
            LogAssert.Expect(LogType.Warning,
                "CardGameMaster missing components: cinematicDirector, soundSystem. Running in degraded mode (tests/minimal setup).");
            LogAssert.Expect(LogType.Log, "Found 0 PlacedCardHolder instances");

            testObject = new GameObject("Test");
            testMaster = testObject.AddComponent<CardGameMaster>();
            testMaster.deckManager = testObject.AddComponent<DeckManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject) Object.DestroyImmediate(testObject);
        }

        [Test]
        public void ModLoader_WithValidMaster_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ModLoader.TryLoadMods(testMaster));
        }

        [Test]
        public void ModLoader_WithNull_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ModLoader.TryLoadMods(null));
        }

        [Test]
        public void ModAssets_HandlesInvalidInputs()
        {
            Assert.DoesNotThrow(() => ModAssets.RegisterBundle("test", null));
            Assert.DoesNotThrow(() => ModAssets.RegisterBundle("", null));
            Assert.DoesNotThrow(() => ModAssets.RegisterBundle(null, null));

            Assert.IsNull(ModAssets.LoadFromBundle<GameObject>("missing", "asset"));
            Assert.IsNull(ModAssets.LoadFromBundle<GameObject>("", "asset"));
            Assert.IsNull(ModAssets.LoadFromBundle<GameObject>("key", ""));
        }


        [Test]
        public void ModAfflictionRegistry_RegisterAndRetrieve()
        {
            // Clear registry to start fresh
            ModAfflictionRegistry.Clear();

            // Create a test affliction
            var testAffliction = new ModAffliction("TestPest", "A test pest", Color.red);

            // Register it
            ModAfflictionRegistry.Register("TestPest", testAffliction);

            // Verify it's registered
            Assert.IsTrue(ModAfflictionRegistry.IsRegistered("TestPest"));
            Assert.AreEqual(1, ModAfflictionRegistry.Count);

            // Retrieve it (should get a clone)
            var retrieved = ModAfflictionRegistry.GetAffliction("TestPest");
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("TestPest", retrieved.Name);
            Assert.AreNotSame(testAffliction, retrieved); // Should be a clone

            // Clean up
            ModAfflictionRegistry.Clear();
        }

        [Test]
        public void ModAfflictionRegistry_HandlesInvalidInputs()
        {
            ModAfflictionRegistry.Clear();

            // Null/empty inputs should not crash
            Assert.DoesNotThrow(() => ModAfflictionRegistry.Register(null, null));
            Assert.DoesNotThrow(() => ModAfflictionRegistry.Register("", null));
            Assert.DoesNotThrow(() => ModAfflictionRegistry.Register("test", null));

            // Should return null for missing afflictions
            Assert.IsNull(ModAfflictionRegistry.GetAffliction("missing"));
            Assert.IsNull(ModAfflictionRegistry.GetAffliction(""));
            Assert.IsNull(ModAfflictionRegistry.GetAffliction(null));

            Assert.IsFalse(ModAfflictionRegistry.IsRegistered("missing"));
            Assert.IsFalse(ModAfflictionRegistry.IsRegistered(""));
            Assert.IsFalse(ModAfflictionRegistry.IsRegistered(null));

            ModAfflictionRegistry.Clear();
        }
    }
}