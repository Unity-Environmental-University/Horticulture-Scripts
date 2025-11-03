using System.Collections;
using _project.Scripts.Audio;
using _project.Scripts.Card_Core;
using _project.Scripts.Cinematics;
using _project.Scripts.Core;
using _project.Scripts.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace _project.Scripts.PlayModeTest
{
    public class UIInputManagerTests
    {
        private GameObject _cgmGo;
        private InputSystemUIInputModule _uiInputModule;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Create CardGameMaster with all dependencies
            _cgmGo = new GameObject("CardGameMaster");
            _cgmGo.AddComponent<DeckManager>();
            _cgmGo.AddComponent<ScoreManager>();
            _cgmGo.AddComponent<TurnController>();
            _cgmGo.AddComponent<SoundSystemMaster>();
            _cgmGo.AddComponent<SaveManager>();
            _cgmGo.AddComponent<CinematicDirector>();
            var cgm = _cgmGo.AddComponent<CardGameMaster>();

            // Create EventSystem with InputSystemUIInputModule
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            _uiInputModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();

            // Assign to CardGameMaster
            cgm.uiInputModule = _uiInputModule;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_cgmGo) Object.Destroy(_cgmGo);
            if (_uiInputModule) Object.Destroy(_uiInputModule.gameObject);

            yield return null;
        }

        [UnityTest]
        public IEnumerator RequestEnable_enables_UIInput()
        {
            UIInputManager.RequestEnable("TestOwner");
            yield return null;

            Assert.IsTrue(_uiInputModule.enabled, "UIInput should be enabled");
            Assert.AreEqual("TestOwner", UIInputManager.CurrentOwner, "Owner should be set");
        }

        [UnityTest]
        public IEnumerator RequestDisable_by_owner_disables_UIInput()
        {
            UIInputManager.RequestEnable("TestOwner");
            yield return null;

            UIInputManager.RequestDisable("TestOwner");
            yield return null;

            Assert.IsFalse(_uiInputModule.enabled, "UIInput should be disabled");
            Assert.IsNull(UIInputManager.CurrentOwner, "Owner should be cleared");
        }

        [UnityTest]
        public IEnumerator RequestDisable_by_non_owner_does_not_disable()
        {
            UIInputManager.RequestEnable("OwnerA");
            yield return null;

            UIInputManager.RequestDisable("OwnerB");
            yield return null;

            Assert.IsTrue(_uiInputModule.enabled, "UIInput should remain enabled when non-owner tries to disable");
            Assert.AreEqual("OwnerA", UIInputManager.CurrentOwner, "Owner should remain unchanged");
        }

        [UnityTest]
        public IEnumerator RequestEnable_transfers_ownership()
        {
            UIInputManager.RequestEnable("OwnerA");
            yield return null;

            UIInputManager.RequestEnable("OwnerB");
            yield return null;

            Assert.IsTrue(_uiInputModule.enabled, "UIInput should remain enabled");
            Assert.AreEqual("OwnerB", UIInputManager.CurrentOwner, "Ownership should transfer to new owner");
        }

        [UnityTest]
        public IEnumerator RaceCondition_popup_after_cinematic_skip()
        {
            // Simulate cinematic enabling UIInput
            UIInputManager.RequestEnable("CutsceneUI");
            yield return null;
            Assert.IsTrue(_uiInputModule.enabled, "Cinematic should enable UIInput");

            // Simulate popup requesting enable BEFORE cinematic OnDisable
            UIInputManager.RequestEnable("PopUpController");
            yield return null;
            Assert.IsTrue(_uiInputModule.enabled, "Popup should take ownership");
            Assert.AreEqual("PopUpController", UIInputManager.CurrentOwner, "Popup should be owner");

            // Simulate cinematic OnDisable trying to disable
            UIInputManager.RequestDisable("CutsceneUI");
            yield return null;

            // Critical assertion: UIInput should REMAIN enabled
            Assert.IsTrue(_uiInputModule.enabled, "UIInput should remain enabled - popup owns it now");
            Assert.AreEqual("PopUpController", UIInputManager.CurrentOwner, "Popup should still be owner");
        }

        [UnityTest]
        public IEnumerator ForceState_overrides_ownership()
        {
            UIInputManager.RequestEnable("OwnerA");
            yield return null;

            UIInputManager.ForceState(false, null, ForcedStateReason.CriticalError);
            yield return null;

            Assert.IsFalse(_uiInputModule.enabled, "ForceState should disable UIInput regardless of owner");
            Assert.IsNull(UIInputManager.CurrentOwner, "Owner should be cleared");
        }

        [UnityTest]
        public IEnumerator ForceState_can_set_new_owner()
        {
            UIInputManager.RequestEnable("OwnerA");
            yield return null;

            UIInputManager.ForceState(true, "OwnerB", ForcedStateReason.SceneTransition);
            yield return null;

            Assert.IsTrue(_uiInputModule.enabled, "UIInput should be enabled");
            Assert.AreEqual("OwnerB", UIInputManager.CurrentOwner, "New owner should be set");
        }

        [UnityTest]
        public IEnumerator RaceCondition_OnDisable_after_ownership_transfer()
        {
            // Simulate: Cinematic enables, popup enables (takes ownership), THEN cinematic OnDisable fires
            UIInputManager.RequestEnable("CutsceneUI");
            yield return null;
            Assert.IsTrue(_uiInputModule.enabled, "Cinematic should enable UIInput");
            Assert.AreEqual("CutsceneUI", UIInputManager.CurrentOwner);

            // Popup takes ownership (this simulates the popup opening before OnDisable fires)
            UIInputManager.RequestEnable("PopUpController");
            yield return null;
            Assert.IsTrue(_uiInputModule.enabled, "Popup should enable UIInput");
            Assert.AreEqual("PopUpController", UIInputManager.CurrentOwner, "Popup should own UIInput");

            // Simulate OnDisable being called AFTER OnEnable (Unity lifecycle quirk)
            // This was the original bug - OnDisable executing after the new system already took over
            UIInputManager.RequestDisable("CutsceneUI");
            yield return null;

            // Critical: UIInput should remain enabled because popup owns it
            Assert.IsTrue(_uiInputModule.enabled, "UIInput should remain enabled after late OnDisable");
            Assert.AreEqual("PopUpController", UIInputManager.CurrentOwner, "Popup should maintain ownership");
        }

        [UnityTest]
        public IEnumerator ReleaseOwnership_clears_owner_without_changing_state()
        {
            UIInputManager.RequestEnable("TestOwner");
            yield return null;

            UIInputManager.ReleaseOwnership("TestOwner");
            yield return null;

            Assert.IsTrue(_uiInputModule.enabled, "UIInput should remain enabled");
            Assert.IsNull(UIInputManager.CurrentOwner, "Owner should be cleared");
        }

        [UnityTest]
        public IEnumerator ReleaseOwnership_by_non_owner_does_nothing()
        {
            UIInputManager.RequestEnable("OwnerA");
            yield return null;

            UIInputManager.ReleaseOwnership("OwnerB");
            yield return null;

            Assert.AreEqual("OwnerA", UIInputManager.CurrentOwner, "Owner should remain unchanged");
        }

        [UnityTest]
        public IEnumerator IsEnabled_reflects_actual_state()
        {
            UIInputManager.RequestEnable("TestOwner");
            yield return null;
            Assert.IsTrue(UIInputManager.IsEnabled, "IsEnabled should return true");

            UIInputManager.RequestDisable("TestOwner");
            yield return null;
            Assert.IsFalse(UIInputManager.IsEnabled, "IsEnabled should return false");
        }

        [UnityTest]
        public IEnumerator MultipleSystemsSequence_ShopToWinScreen()
        {
            // Simulate shop opening
            UIInputManager.RequestEnable("ShopManager");
            yield return null;
            Assert.IsTrue(_uiInputModule.enabled, "Shop should enable UIInput");

            // Shop closes
            UIInputManager.RequestDisable("ShopManager");
            yield return null;
            Assert.IsFalse(_uiInputModule.enabled, "UIInput should be disabled after shop");

            // Win screen shows
            UIInputManager.RequestEnable("TurnController");
            yield return null;
            Assert.IsTrue(_uiInputModule.enabled, "Win screen should enable UIInput");
            Assert.AreEqual("TurnController", UIInputManager.CurrentOwner);
        }
    }
}
