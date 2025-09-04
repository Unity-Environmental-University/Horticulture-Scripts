using NUnit.Framework;
using UnityEngine;
using _project.Scripts.Card_Core;
using _project.Scripts.ModLoader;

namespace _project.Scripts.PlayModeTest
{
    /// <summary>
    /// Simple tests for the simplified ModLoader
    /// </summary>
    public class ModLoaderTest
    {
        private GameObject testObject;
        private CardGameMaster testMaster;
        
        [SetUp]
        public void SetUp()
        {
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
        public void ModInfo_HandlesInvalidJson()
        {
            var info = ModInfo.FromJson("");
            Assert.IsNotNull(info);
            Assert.AreEqual("Unknown Mod", info.name);
            
            info = ModInfo.FromJson("invalid json");
            Assert.IsNotNull(info);
            
            info = ModInfo.FromJson(@"{""name"":""Test""}");
            Assert.AreEqual("Test", info.name);
        }
    }
}