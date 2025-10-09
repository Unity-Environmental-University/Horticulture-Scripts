using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Audio;
using _project.Scripts.Card_Core;
using _project.Scripts.Cinematics;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.Stickers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace _project.Scripts.PlayModeTest
{
    public class CardHolderVisibilityTests
    {
        private GameObject _cgmGo;
        private DeckManager _deckManager;
        private Transform _location;

        // Safe subclass of Click3D that disables Start logic.
        private class SafeClick3D : Click3D
        {
            // ReSharper disable once Unity.RedundantEventFunction
            private void Start() { /* No-op to prevent self-destruction check from running */ }
        }

        private class FakeLocationCard : ILocationCard
        {
            public string Name => "Fake Location";
            public string Description => "Test location";
            public int? Value => 1;
            public Material Material => null;
            public List<ISticker> Stickers { get; } = new();
            public bool IsPermanent => false;
            public int EffectDuration => 1;
            public LocationEffectType EffectType => null;
            public ICard Clone() => this; // Not used in tests
            public void ApplyLocationEffect(PlantController plant) { }
            public void RemoveLocationEffect(PlantController plant) { }
            public void ApplyTurnEffect(PlantController plant) { }
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _cgmGo = new GameObject("CardGameMaster");
            // Add in dependency order so Awake finds them
            _deckManager = _cgmGo.AddComponent<DeckManager>();
            _cgmGo.AddComponent<ScoreManager>();
            _cgmGo.AddComponent<TurnController>();
            _cgmGo.AddComponent<SoundSystemMaster>();
            _cgmGo.AddComponent<SaveManager>();
            _cgmGo.AddComponent<CinematicDirector>();
            _cgmGo.AddComponent<CardGameMaster>();

            _location = new GameObject("Location").transform;
            _deckManager.plantLocations = new List<Transform> { _location };

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_cgmGo) Object.Destroy(_cgmGo);
            if (_location) Object.Destroy(_location.gameObject);
            yield return null;
        }

        private static PlacedCardHolder CreateHolder(Transform parent, bool withButton = true)
        {
            var holderGo = new GameObject("CardHolder");
            holderGo.transform.SetParent(parent);
            var holder = holderGo.AddComponent<PlacedCardHolder>();

            if (withButton)
            {
                var button = new GameObject("Button");
                button.transform.SetParent(holderGo.transform);
                button.AddComponent<MeshRenderer>();
                button.AddComponent<SafeClick3D>();
            }

            return holder;
        }

        private static void GiveHolderCard(PlacedCardHolder holder)
        {
            var cardGo = new GameObject("CardClone");
            cardGo.transform.SetParent(holder.transform);
            var click = cardGo.AddComponent<SafeClick3D>();
            typeof(PlacedCardHolder)
                .GetField("placedCardClick3D", BindingFlags.Public | BindingFlags.Instance)
                ?.SetValue(holder, click);

            // Place a fake location card to mimic persistence
            typeof(PlacedCardHolder)
                .GetField("placedCard", BindingFlags.Public | BindingFlags.Instance)
                ?.SetValue(holder, new FakeLocationCard());
        }

        [UnityTest]
        public IEnumerator ClearAllPlants_keeps_holder_visible_when_holding_card()
        {
            var holder = CreateHolder(_location);
            holder.ToggleCardHolder(true);
            GiveHolderCard(holder);

            // Act
            _deckManager.ClearAllPlants();
            yield return null;

            var buttonRenderer = holder.transform.Find("Button").GetComponent<MeshRenderer>();
            var buttonClick = holder.transform.Find("Button").GetComponent<Click3D>();
            Assert.IsNotNull(buttonRenderer, "Button MeshRenderer missing");
            Assert.IsTrue(buttonRenderer.enabled, "Holder button renderer should remain enabled when holding a card");
            Assert.IsNotNull(buttonClick, "Button Click3D missing");
            Assert.IsTrue(buttonClick.isEnabled, "Holder button Click3D should remain enabled when holding a card");
        }

        [UnityTest]
        public IEnumerator ClearPlant_keeps_holder_visible_when_holding_card()
        {
            // Create plant under location
            var plantGo = new GameObject("Plant");
            plantGo.transform.SetParent(_location);
            var plant = plantGo.AddComponent<PlantController>();
            var ps = plantGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false; // avoid auto-play
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            main.duration = 0.01f; // minimize wait
            plant.deathFX = ps;

            var holder = CreateHolder(_location);
            holder.ToggleCardHolder(true);
            GiveHolderCard(holder);

            // Act
            yield return _deckManager.ClearPlant(plant);

            var buttonRenderer = holder.transform.Find("Button").GetComponent<MeshRenderer>();
            var buttonClick = holder.transform.Find("Button").GetComponent<Click3D>();
            Assert.IsNotNull(buttonRenderer, "Button MeshRenderer missing");
            Assert.IsTrue(buttonRenderer.enabled, "Holder button renderer should remain enabled when holding a card");
            Assert.IsNotNull(buttonClick, "Button Click3D missing");
            Assert.IsTrue(buttonClick.isEnabled, "Holder button Click3D should remain enabled when holding a card");
        }

        [UnityTest]
        public IEnumerator LocationCardExpiry_hides_holder_when_no_plant()
        {
            var holder = CreateHolder(_location);
            holder.ToggleCardHolder(true);
            GiveHolderCard(holder);

            // Act
            holder.ClearLocationCardByExpiry();
            yield return null;

            var buttonRenderer = holder.transform.Find("Button").GetComponent<MeshRenderer>();
            var buttonClick = holder.transform.Find("Button").GetComponent<Click3D>();
            Assert.IsNotNull(buttonRenderer, "Button MeshRenderer missing");
            Assert.IsFalse(buttonRenderer.enabled, "Holder should hide when location card expires and no plant is present");
            Assert.IsNotNull(buttonClick, "Button Click3D missing");
            Assert.IsFalse(buttonClick.isEnabled, "Holder Click3D should be disabled when hidden");
        }

        [UnityTest]
        public IEnumerator LocationCardExpiry_shows_holder_when_plant_present()
        {
            // Create plant under location
            var plantGo = new GameObject("Plant");
            plantGo.transform.SetParent(_location);
            plantGo.AddComponent<PlantController>();

            var holder = CreateHolder(_location);
            holder.ToggleCardHolder(false);
            GiveHolderCard(holder);

            // Act
            holder.ClearLocationCardByExpiry();
            yield return null;

            var buttonRenderer = holder.transform.Find("Button").GetComponent<MeshRenderer>();
            var buttonClick = holder.transform.Find("Button").GetComponent<Click3D>();
            Assert.IsNotNull(buttonRenderer, "Button MeshRenderer missing");
            Assert.IsTrue(buttonRenderer.enabled, "Holder should show when location card expires but a plant is present");
            Assert.IsNotNull(buttonClick, "Button Click3D missing");
            Assert.IsTrue(buttonClick.isEnabled, "Holder Click3D should be enabled when shown");
        }
    }
}
