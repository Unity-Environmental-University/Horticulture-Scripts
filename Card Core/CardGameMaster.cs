using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    /// <summary>
    ///     Represents the core manager of the card game, responsible for integrating key components such as deck management,
    ///     scoring, and turn control. This class acts as the central hub for game operations.
    /// </summary>
    /// <remarks>
    ///     The <c>CardGameMaster</c> is attached to a Unity GameObject and requires <c>DeckManager</c>, <c>ScoreManager</c>,
    ///     and <c>TurnController</c> components. It also maintains a singleton instance for centralized access across the
    ///     game.
    /// </remarks>
    [RequireComponent(typeof(DeckManager))]
    [RequireComponent(typeof(ScoreManager))]
    [RequireComponent(typeof(TurnController))]
    public class CardGameMaster : MonoBehaviour
    {
        public DeckManager deckManager;
        public ScoreManager scoreManager;
        public TurnController turnController;
        public List<PlacedCardHolder> cardHolders;

        [CanBeNull] public TextMeshPro scoreText;
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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Double Check Controllers
            if (!scoreManager || !deckManager || !turnController)
                throw new Exception("Must assign score/deck/turn manager");

            // Find all CardHolders
            cardHolders =
                new List<PlacedCardHolder>(
                    FindObjectsByType<PlacedCardHolder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));

            Instance = this;
            // DontDestroyOnLoad(gameObject);
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