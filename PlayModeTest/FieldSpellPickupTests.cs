using System.Collections;
using System.Reflection;
using _project.Scripts.Audio;
using _project.Scripts.Card_Core;
using _project.Scripts.Cinematics;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace _project.Scripts.PlayModeTest
{
    public class FieldSpellPickupTests
    {
        private GameObject _cgmGo;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _cgmGo = new GameObject("CardGameMaster");
            _cgmGo.AddComponent<DeckManager>();
            _cgmGo.AddComponent<ScoreManager>();
            _cgmGo.AddComponent<TurnController>();
            _cgmGo.AddComponent<SoundSystemMaster>();
            _cgmGo.AddComponent<SaveManager>();
            _cgmGo.AddComponent<CinematicDirector>();
            _cgmGo.AddComponent<CardGameMaster>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_cgmGo) Object.Destroy(_cgmGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TakeSelectedCard_does_not_unplace_field_spell()
        {
            var holderGo = new GameObject("CardHolder");
            var holder = holderGo.AddComponent<PlacedCardHolder>();
            yield return null;

            var cardClone = new GameObject("CardClone");
            cardClone.transform.SetParent(holder.transform);
            holder.placedCardClick3D = cardClone.AddComponent<SafeClick3D>();
            var fieldSpell = new LadyBugsCard();
            holder.placedCard = fieldSpell;

            holder.TakeSelectedCard();
            yield return null;

            Assert.IsTrue(holder.HoldingCard, "Holder should still report HoldingCard after TakeSelectedCard.");
            Assert.AreSame(fieldSpell, holder.placedCard, "Field spell should not be unplaced by TakeSelectedCard.");

            Object.Destroy(holderGo);
        }

        [UnityTest]
        public IEnumerator OnPlacedCardClicked_does_not_swap_or_pickup_field_spell()
        {
            var holderGo = new GameObject("CardHolder");
            var holder = holderGo.AddComponent<PlacedCardHolder>();
            yield return null;

            var cardClone = new GameObject("CardClone");
            cardClone.transform.SetParent(holder.transform);
            holder.placedCardClick3D = cardClone.AddComponent<SafeClick3D>();
            var fieldSpell = new LadyBugsCard();
            holder.placedCard = fieldSpell;

            var deckManager = CardGameMaster.Instance.deckManager;
            deckManager.selectedACard = new HorticulturalOilBasic();
            deckManager.selectedACardClick3D = new GameObject("SelectedCard").AddComponent<SafeClick3D>();

            var clickMethod = typeof(PlacedCardHolder).GetMethod("OnPlacedCardClicked",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(clickMethod, "Could not reflect PlacedCardHolder.OnPlacedCardClicked");
            clickMethod.Invoke(holder, null);

            Assert.IsTrue(holder.HoldingCard, "Holder should still report HoldingCard after OnPlacedCardClicked.");
            Assert.AreSame(fieldSpell, holder.placedCard, "Field spell should not be swapped/picked up by clicking.");

            Object.Destroy(deckManager.selectedACardClick3D.gameObject);
            Object.Destroy(holderGo);
        }

        private class SafeClick3D : Click3D
        {
            // ReSharper disable once Unity.RedundantEventFunction
            private void Start()
            {
                /* No-op to prevent self-destruction check from running */
            }
        }
    }
}

