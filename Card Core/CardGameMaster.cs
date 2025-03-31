using System;
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

        [CanBeNull] public TextMeshPro scoreText;
        public static CardGameMaster Instance { get; private set; }

        /// <summary>
        ///     Initializes the CardGameMaster instance and ensures the singleton pattern is followed.
        ///     Checks and validates that required components (DeckManager, ScoreManager, TurnController)
        ///     are properly assigned. Prevents multiple instances by destroying duplicates.
        ///     Ensures the GameObject persists across scenes.
        /// </summary>
        /// <exception cref="Exception">
        ///     Thrown when any of the required components (DeckManager, ScoreManager, or TurnController)
        ///     are not assigned.
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

            Instance = this;
            DontDestroyOnLoad(gameObject);
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