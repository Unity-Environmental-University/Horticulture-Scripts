using System;
using System.Collections.Generic;
using _project.Scripts.Audio;
using _project.Scripts.Cinematics;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    /// Represents the primary controller for managing the card game's overall functionality,
    /// including integration with deck, score, turn, and other essential systems.
    /// </summary>
    /// <remarks>
    /// The <c>CardGameMaster</c> class ensures seamless communication between core components such as
    /// <c>DeckManager</c>, <c>ScoreManager</c>, and <c>TurnController</c>. It is designed to manage singleton
    /// behavior, providing centralized access throughout the game's lifecycle. This class interacts with
    /// mission-critical elements, including UI updates, audio playback, and game state control.
    /// </remarks>
    [RequireComponent(typeof(DeckManager))]
    [RequireComponent(typeof(ScoreManager))]
    [RequireComponent(typeof(TurnController))]
    public class CardGameMaster : MonoBehaviour
    {
        [Header("FOR DEBUGGING ONLY")]
        [Tooltip("Turning this off skips Most Story elements. ONLY SET THIS TO FALSE DURING DEVELOPMENT")]
        public static bool IsSequencingEnabled => PlayerPrefs.GetInt("Tutorial", 1) == 1;

        [Space (20)]
        public bool isInspecting;
        public bool debuggingCardClass;
        
        [Header("Major Game Components")]
        public DeckManager deckManager;
        public ScoreManager scoreManager;
        public TurnController turnController;
        public ShopManager shopManager;
        public SoundSystemMaster soundSystem;
        public CinematicDirector cinematicDirector;
        public AudioSource playerHandAudioSource;
        // ReSharper disable once UnusedMember.Global
        public AudioSource robotAudioSource;
        
        [Space(5)]
        [Header("Objects")]
        public Volume postProcessVolume;
        public InputSystemUIInputModule uiInputModule;
        public GameObject inspectingInfoPanels;
        
        [Space(5)]
        [Header("Text Items")]
        [CanBeNull] public TextMeshProUGUI shopMoneyText;
        [CanBeNull] public TextMeshPro moneysText;
        [CanBeNull] public TextMeshPro turnText;
        [CanBeNull] public TextMeshPro roundText;
        [CanBeNull] public TextMeshPro levelText;
        [CanBeNull] public TextMeshPro treatmentCostText;
        [CanBeNull] public TextMeshPro potentialProfitText;

        [Header("Object Arrays/Lists")]
        public List<PlacedCardHolder> cardHolders;
        public static CardGameMaster Instance { get; private set; }

        /// <summary>
        /// Initializes the CardGameMaster instance and ensures the singleton pattern is correctly implemented.
        /// Verifies that essential components (DeckManager, ScoreManager, TurnController) are properly assigned.
        /// Prevents the existence of multiple duplicate instances by destroying any duplicates found.
        /// Scans and stores all instances of PlacedCardHolder in the current scene.
        /// Ensures the GameObject persists across scenes during transitions.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown when one or more required components (DeckManager, ScoreManager, TurnController)
        /// are not assigned.
        /// </exception>
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Double Check Controllers
            if (!scoreManager || !deckManager || !turnController || !cinematicDirector || !soundSystem)
                throw new Exception("Crucial Component Missing!");

            // Find all CardHolders
            cardHolders =
                new List<PlacedCardHolder>(
                    FindObjectsByType<PlacedCardHolder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));

            Instance = this;
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
