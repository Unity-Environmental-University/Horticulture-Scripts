using System;
using System.Collections.Generic;
using _project.Scripts.Audio;
using _project.Scripts.Cinematics;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.GameState;
using _project.Scripts.Handlers;
using _project.Scripts.ModLoading;
using _project.Scripts.Rendering;
using _project.Scripts.UI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    /// Central controller for the card game system. Manages integration between deck, score, turn, and other core systems.
    /// Implements singleton pattern for global access.
    /// </summary>
    [RequireComponent(typeof(DeckManager))]
    [RequireComponent(typeof(ScoreManager))]
    [RequireComponent(typeof(TurnController))]
    [RequireComponent(typeof(CardSelectionOutlineController))]
    public class CardGameMaster : MonoBehaviour
    {
        [Space(20)] public bool isInspecting;

        public bool debuggingCardClass;

        [Header("Major Game Components")] public DeckManager deckManager;

        public ScoreManager scoreManager;
        public TurnController turnController;
        public ShopManager shopManager;
        public SoundSystemMaster soundSystem;
        public CinematicDirector cinematicDirector;
        public PopUpController popUpController;
        public CardSelectionOutlineController selOutlineController;
        public TreatmentEfficacyHandler treatmentEfficacyHandler;
        public AudioSource playerHandAudioSource;
        public SaveManager saveManager;

        // ReSharper disable once UnusedMember.Global
        public AudioSource robotAudioSource;

        [Space(5)] [Header("Objects")]
        public Volume postProcessVolume;
        public InputSystemUIInputModule uiInputModule;
        public GameObject inspectingInfoPanels;

        [Space(5)] [Header("Referencable Objects")]
        public GameObject actionCardPrefab;
        public GameObject locationCardPrefab;

        [Space(5)] [Header("Text Items")] [CanBeNull]
        public TextMeshProUGUI shopMoneyText;

        [CanBeNull] public TextMeshPro moneysText;
        [CanBeNull] public TextMeshPro turnText;
        [CanBeNull] public TextMeshPro roundText;
        [CanBeNull] public TextMeshPro levelText;
        [CanBeNull] public TextMeshPro treatmentCostText;
        [CanBeNull] public TextMeshPro potentialProfitText;

        [Header("Object Arrays/Lists")] public List<PlacedCardHolder> cardHolders;

        [Header("Non-Static Objects")] public InspectFromClick inspectedObj;

        [Header("FOR DEBUGGING ONLY")]
        [Tooltip("Turning this off skips Most Story elements. ONLY SET THIS TO FALSE DURING DEVELOPMENT")]
        public static bool IsSequencingEnabled => PlayerPrefs.GetInt("Tutorial", 1) == 1;

        public static CardGameMaster Instance { get; private set; }

        /// <summary>
        /// Initializes singleton instance and required components. Scans for PlacedCardHolder instances.
        /// </summary>
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (!scoreManager) scoreManager = GetComponent<ScoreManager>();
            if (!deckManager) deckManager = GetComponent<DeckManager>();
            if (!turnController) turnController = GetComponent<TurnController>();
            if (!cinematicDirector) cinematicDirector = GetComponent<CinematicDirector>();
            if (!soundSystem) soundSystem = GetComponent<SoundSystemMaster>();
            saveManager ??= new SaveManager();

            var missing = new List<string>();
            if (!scoreManager) missing.Add(nameof(scoreManager));
            if (!deckManager) missing.Add(nameof(deckManager));
            if (!turnController) missing.Add(nameof(turnController));
            if (!cinematicDirector) missing.Add(nameof(cinematicDirector));
            if (!soundSystem) missing.Add(nameof(soundSystem));
            if (missing.Count > 0)
                Debug.LogWarning(
                    $"CardGameMaster missing components: {string.Join(", ", missing)}. Running in degraded mode (tests/minimal setup).");

            try
            {
                var foundHolders =
                    FindObjectsByType<PlacedCardHolder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                cardHolders = foundHolders != null
                    ? new List<PlacedCardHolder>(foundHolders)
                    : new List<PlacedCardHolder>();
                Debug.Log($"Found {cardHolders.Count} PlacedCardHolder instances");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error finding PlacedCardHolder instances: {e.Message}");
                cardHolders = new List<PlacedCardHolder>();
            }

            // Load user mods (cards/stickers) before deck initialization runs in Start()
            try
            {
                ModLoader.TryLoadMods(this);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Mod loading failed: {e.Message}");
            }
        }

        /// <summary>
        ///     Destroys the GameObject this script is attached to at runtime.
        ///     This action removes the instance of CardGameMaster from the scene,
        ///     effectively cleaning up any associated data and components.
        /// </summary>
        public void SelfDestruct()
        {
            Destroy(gameObject);
        }
    }
}
