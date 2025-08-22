using System;
using System.Collections.Generic;
using _project.Scripts.Audio;
using _project.Scripts.Cinematics;
using _project.Scripts.Core;
using _project.Scripts.GameState;
using _project.Scripts.UI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    ///     Represents the primary controller for managing the card game's overall functionality,
    ///     including integration with deck, score, turn, and other essential systems.
    /// </summary>
    /// <remarks>
    ///     The <c>CardGameMaster</c> class ensures seamless communication between core components such as
    ///     <c>DeckManager</c>, <c>ScoreManager</c>, and <c>TurnController</c>. It is designed to manage singleton
    ///     behavior, providing centralized access throughout the game's lifecycle. This class interacts with
    ///     mission-critical elements, including UI updates, audio playback, and game state control.
    /// </remarks>
    [RequireComponent(typeof(DeckManager))]
    [RequireComponent(typeof(ScoreManager))]
    [RequireComponent(typeof(TurnController))]
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
        public AudioSource playerHandAudioSource;

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
        ///     Initializes the CardGameMaster instance and ensures the singleton pattern is correctly implemented.
        ///     Verifies that essential components (DeckManager, ScoreManager, TurnController) are properly assigned.
        ///     Prevents the existence of multiple duplicate instances by destroying any duplicates found.
        ///     Scans and stores all instances of PlacedCardHolder in the current scene.
        ///     Ensures the GameObject persists across scenes during transitions.
        /// </summary>
        /// <exception cref="Exception">
        ///     Thrown when one or more required components (DeckManager, ScoreManager, TurnController)
        ///     are not assigned.
        /// </exception>
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
        }

        public void Save()
        {
            try
            {
                GameStateManager.SaveGame();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }

        public void Load()
        {
            try
            {
                GameStateManager.LoadGame();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
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