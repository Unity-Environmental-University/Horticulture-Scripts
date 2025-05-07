using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using UnityEngine;
using Random = System.Random;

namespace _project.Scripts.Card_Core
{
    public class TurnController : MonoBehaviour
    {
        private ScoreManager _scoreManager;
        private DeckManager _deckManager;
        public GameObject lostGameObjects;
        public int turnCount = 4;
        public int currentTurn;
        public int currentRound;
        public bool canClickEnd;
        public bool newRoundReady;
        public bool debugging;
        private static TurnController Instance { get; set; }


        private void Awake()
        {
            _deckManager = CardGameMaster.Instance.deckManager;
            _scoreManager = CardGameMaster.Instance.scoreManager;
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            canClickEnd = false;
        }

        private void Start() { StartCoroutine(BeginTurnSequence()); }

        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator BeginTurnSequence()
        {
            if (lostGameObjects.activeInHierarchy) lostGameObjects.SetActive(false);
            canClickEnd = false;
            currentTurn = 1;
            currentRound++;
            
            yield return new WaitForSeconds(2f);
            try
            {
                _deckManager.PlacePlants();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            yield return new WaitForSeconds(1f);
            try
            {
                _deckManager.DrawAfflictions();
                _deckManager.DrawActionHand();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            yield return new WaitForSeconds(.5f);
            canClickEnd = true;
        }

        private void Update()
        {
            if (CardGameMaster.Instance.turnText)
                CardGameMaster.Instance.turnText.text = "Turn: " + currentTurn + "/" + turnCount;
        }

        /// <summary>
        ///     Ends the current turn, updates the game state, and prepares for the next turn or round as needed.
        /// </summary>
        /// <remarks>
        ///     This method performs the following operations:
        ///     1. Checks if the action display is still updating or if the "End Turn" button is not clickable. If either is true,
        ///     the method returns early.
        ///     2. Verify if a new round is ready. If so, prepare for the next round by resetting the round state and starting the
        ///     turn sequence, then returns.
        ///     3. Retrieve all active plant controllers from the defined plant locations.
        ///     4. For each plant controller:
        ///     a. Applies all queued treatments.
        ///     B. Flags the associated shaders for an update.
        ///     5. If all plants are free of afflictions, end the current round early.
        ///     6. If the maximum turn count has not been reached:
        ///     a. Increments the turn counter for tracking progress.
        ///     B. Iterates through plant controllers to evaluate affliction spread to neighboring plants, considering randomized
        ///     probabilities and valid neighboring plants.
        ///     C. Draws an action hand for the next turn.
        ///     7. If the maximum turn count has been reached, ends the current round.
        /// </remarks>
        /// <exception cref="System.NullReferenceException">
        ///     Thrown if `deckManager`, `plantLocations`, or any dependencies (e.g., `PlantController` or `PlantCardFunctions`)
        ///     are not properly initialized.
        /// </exception>
        public void EndTurn()
        {
            if (_deckManager.updatingActionDisplay || !canClickEnd) return;

            // If we're ready for a new round, call setup and return
            if (newRoundReady)
            {
                newRoundReady = false;
                StartCoroutine(BeginTurnSequence());
                return;
            }

            // Get an array of plant controllers
            var plantControllers = _deckManager.plantLocations
                .SelectMany(location => location.GetComponentsInChildren<PlantController>(false))
                .ToArray();
            
            var cardHolders = CardGameMaster.Instance?.cardHolders;
            if (cardHolders != null)
            {
                var treatmentCost = cardHolders.Where(cardHolder => cardHolder.PlacedCard?.Value != null)
                    .Sum(cardHolder => cardHolder.PlacedCard.Value.Value);

                if (debugging) Debug.Log($"Found {plantControllers.Length} PlantControllers in PlantLocation.");

                // Apply queued treatments and update shaders
                foreach (var controller in plantControllers)
                {
                    controller.plantCardFunctions.ApplyQueuedTreatments();
                    StartCoroutine(PauseRoutine());
                    controller.FlagShadersUpdate();
                }
            }

            // End round early if all plants are free of afflictions
            if (plantControllers.All(controller => !controller.CurrentAfflictions.Any()))
            {
                StartCoroutine(EndRound());
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
                        if (target.HasAffliction(affliction)) continue;
                        
                        // Don't spread to something that's been given the Panacea
                        if (!target.UsedTreatments.Any(treatment => treatment is PlantAfflictions.Panacea))
                        {
                            target.AddAffliction(affliction);
                            _scoreManager.CalculateTreatmentCost();
                        }
                        
                        StartCoroutine(PauseRoutine());
                        target.FlagShadersUpdate();
                    }

                    if (debugging)
                        Debug.Log(
                            $"Affliction {affliction} spread from {controller.name} to {plantControllers[i].name}.");
                }

                _deckManager.DrawActionHand();
            }
            else
            {
                StartCoroutine(EndRound());
            }
        }

        private static IEnumerator PauseRoutine(float delay = 1f) { yield return new WaitForSeconds(delay); }


        /// <summary>
        ///     Ends the current round, updates the game state, and prepares for the next round.
        /// </summary>
        /// <remarks>
        ///     This method performs the following operations:
        ///     1. Checks if the action display is still updating or if the "End Turn" button is not clickable. If either is true,
        ///     the method returns early.
        ///     2. Verify if a new round is ready. If so, prepare for the next round by resetting the round state and starting the
        ///     turn sequence, then returns.
        ///     3. Retrieve all active plant controllers from the defined plant locations.
        ///     4. For each plant controller:
        ///     a. Applies all queued treatments.
        ///     B. Flags the associated shaders for an update.
        ///     5. If all plants are free of afflictions, end the current round early.
        ///     6. If the maximum turn count has not been reached:
        ///     a. Increments the turn counter for tracking progress.
        ///     B. Iterates through plant controllers to evaluate affliction spread to neighboring plants, considering randomized
        ///     probabilities and valid neighboring plants.
        ///     C. Draws an action hand for the next turn.
        ///     7. If the maximum turn count has been reached, ends the current round.
        /// </remarks>
        /// <exception cref="System.NullReferenceException">
        ///     Thrown if `deckManager`, `plantLocations`, or any dependencies (e.g., `PlantController` or `PlantCardFunctions`)
        ///     are not properly initialized.
        /// </exception>
        private IEnumerator EndRound(float delayTime = 2f)
        {
            canClickEnd = false;
            currentTurn = 0;
            Debug.Log($"Treatment Cost: {_scoreManager.treatmentCost}");
            _deckManager.ClearActionHand();
            _scoreManager.CalculateTreatmentCost();

            yield return new WaitForSeconds(delayTime);

            var score = _scoreManager.CalculateScore();
            if (_scoreManager) _scoreManager.treatmentCost = 0;

            _deckManager.ClearAllPlants();

            Debug.Log("Score: " + score);

            var plantControllers = _deckManager.plantLocations
                .SelectMany(location => location.GetComponentsInChildren<PlantController>(false))
                .ToArray();

            if (debugging) Debug.Log($"Found {plantControllers.Length} PlantControllers in PlantLocation.");

            foreach (var controller in plantControllers)
            {
                controller.plantCardFunctions.ApplyQueuedTreatments();
                controller.FlagShadersUpdate();
            }

            if (score > 0)
            {
                newRoundReady = true;
                canClickEnd = true;
            }
            else
            {
                GameLost();
            }
        }

        public void ResetGame()
        {
            currentRound = 0;
            StartCoroutine(BeginTurnSequence());
            _scoreManager.ResetScore();
        }

        private void GameLost() { lostGameObjects.SetActive(true); }
    }
}