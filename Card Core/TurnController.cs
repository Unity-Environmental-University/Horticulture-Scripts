using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Core;
using TMPro;
using UnityEngine;
using Random = System.Random;

namespace _project.Scripts.Card_Core
{
    public class TurnController : MonoBehaviour
    {
        public ScoreManager scoreManager;
        public DeckManager deckManager;
        public TextMeshPro turnText;
        public int turnCount = 4;
        public int currentTurn;
        public bool canClickEnd;
        public bool debugging;
        private bool _newRoundReady;
        private static TurnController Instance { get; set; }


        private void Awake()
        {
            deckManager = CardGameMaster.Instance.deckManager;
            scoreManager = CardGameMaster.Instance.scoreManager;
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start() { StartCoroutine(BeginTurnSequence()); }

        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator BeginTurnSequence()
        {
            canClickEnd = false;
            currentTurn = 1;
            yield return new WaitForSeconds(2f);
            try
            {
                deckManager.PlacePlants();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            yield return new WaitForSeconds(1f);
            try
            {
                deckManager.DrawAfflictions();
                deckManager.DrawActionHand();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            yield return new WaitForSeconds(.5f);
            canClickEnd = true;
        }

        private void Update() { turnText.text = "Turn: " + currentTurn; }

        /// <summary>
        /// Ends the current turn, updates the game state, and prepares for the next turn or round as needed.
        /// </summary>
        /// <remarks>
        /// This method performs the following operations:
        /// 1. Checks if the action display is still updating or if the "End Turn" button is not clickable. If either is true, the method returns early.
        /// 2. Verifies if a new round is ready. If so, prepares for the next round by resetting the round state and starting the turn sequence, then returns.
        /// 3. Retrieves all active plant controllers from the defined plant locations.
        /// 4. For each plant controller:
        /// a. Applies all queued treatments.
        /// B. Flags the associated shaders for an update.
        /// 5. If all plants are free of afflictions, ends the current round early.
        /// 6. If the maximum turn count has not been reached:
        /// a. Increments the turn counter for tracking progress.
        /// B. Iterates through plant controllers to evaluate affliction spread to neighboring plants, considering randomized probabilities and valid neighboring plants.
        /// c. Draws an action hand for the next turn.
        /// 7. If the maximum turn count has been reached, ends the current round.
        /// </remarks>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if `deckManager`, `plantLocations`, or any dependencies (e.g., `PlantController` or `PlantCardFunctions`) are not properly initialized.
        /// </exception>
        public void EndTurn()
        {
            if (deckManager.updatingActionDisplay || !canClickEnd) return;

            // If we're ready for a new round, call setup and return
            if (_newRoundReady)
            {
                _newRoundReady = false;
                StartCoroutine(BeginTurnSequence());
                return;
            }

            // Get an array of plant controllers
            var plantControllers = deckManager.plantLocations
                .SelectMany(location => location.GetComponentsInChildren<PlantController>(false))
                .ToArray();

            if (debugging) Debug.Log($"Found {plantControllers.Length} PlantControllers in PlantLocation.");

            // Apply queued treatments and update shaders
            foreach (var controller in plantControllers)
            {
                controller.plantCardFunctions.ApplyQueuedTreatments();
                controller.FlagShadersUpdate();
            }

            // End round early if all plants are free of afflictions
            if (plantControllers.All(controller => !controller.CurrentAfflictions.Any()))
            {
                EndRound();
                return;
            }

            if (currentTurn < turnCount)
            {
                currentTurn++;

                var random = new Random();

                // Process each plant controller
                for (var i = 0; i < plantControllers.Length; i++)
                {
                    var controller = plantControllers[i];
                    // Skip if no afflictions or 50% chance
                    if (!controller.CurrentAfflictions.Any() || random.NextDouble() >= 0.5) continue;

                    // Get first affliction from a plant
                    var affliction = controller.CurrentAfflictions.First();

                    // Track possible neighbors to spread to
                    var neighborOptions = new List<PlantController>();

                    // Check left neighbor if exists
                    if (i > 0)
                    {
                        var leftNeighbor = plantControllers[i - 1];
                        // Add if a neighbor exists and doesn't have affliction
                        if (leftNeighbor && !leftNeighbor.HasAffliction(affliction)) neighborOptions.Add(leftNeighbor);
                    }

                    // Check right neighbor if exists
                    if (i < plantControllers.Length - 1)
                    {
                        var rightNeighbor = plantControllers[i + 1];
                        // Add if a neighbor exists and doesn't have affliction
                        if (rightNeighbor && !rightNeighbor.HasAffliction(affliction))
                            neighborOptions.Add(rightNeighbor);
                    }

                    // Spread affliction to a random neighbor if any available
                    if (neighborOptions.Count > 0)
                    {
                        var target = neighborOptions[random.Next(neighborOptions.Count)];
                        target.AddAffliction(affliction);
                        target.FlagShadersUpdate();
                    }

                    if (debugging)
                        Debug.Log(
                            $"Affliction {affliction} spread from {controller.name} to {plantControllers[i].name}.");
                }

                deckManager.DrawActionHand();
            }
            else
            {
                deckManager.ClearAllPlants();
                EndRound();
            }
        }

        /// <summary>
        /// Ends the current round and resets the relevant game state for the next round preparation phase.
        /// </summary>
        /// <remarks>
        /// This method performs the following operations in sequence:
        /// 1. Resets the turn counter to 0 in preparation for a new round.
        /// 2. Clears the current action hand, deck, and discard pile by invoking the `ClearActionHand` method in the `DeckManager`.
        /// 3. Calculates and logs the player's score using the `ScoreManager`.
        /// 4. Retrieves all plant controllers across the defined plant locations and performs the following actions:
        /// a. Applies all queued treatments to each plant controller, ensuring pending effects are resolved.
        /// B. Flags the shaders associated with each plant for an update.
        /// 5. Sets the `_newRoundReady` flag to `true`, indicating readiness for the next game's round setup.
        /// </remarks>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if `deckManager`, `scoreManager`, or any of their required components are not properly initialized.
        /// </exception>
        private void EndRound()
        {
            currentTurn = 0;
            deckManager.ClearAllPlants();
            deckManager.ClearActionHand();
            var score = scoreManager.CalculateScore();
            Debug.Log("Score: " + score);

            var plantControllers = deckManager.plantLocations
                .SelectMany(location => location.GetComponentsInChildren<PlantController>(false))
                .ToArray();

            if (debugging) Debug.Log($"Found {plantControllers.Length} PlantControllers in PlantLocation.");

            foreach (var controller in plantControllers)
            {
                controller.plantCardFunctions.ApplyQueuedTreatments();
                controller.FlagShadersUpdate();
            }

            _newRoundReady = true;
        }
    }
}