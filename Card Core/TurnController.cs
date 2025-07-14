using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using Random = System.Random;

namespace _project.Scripts.Card_Core
{
    public class TurnController : MonoBehaviour
    {
        public GameObject lostGameObjects;
        public GameObject winScreen;
        public int turnCount = 4;
        public int level;
        public int moneyGoal;
        public int currentTurn;
        public int totalTurns;
        public int currentRound;
        public bool canClickEnd;
        public bool newRoundReady;
        public bool debugging;
        public bool shopQueued;

        public Func<bool> ReadyToPlay;
        private static readonly Queue<PlantEffectRequest> PlantEffectQueue = new(); 
        private DeckManager _deckManager;
        private ScoreManager _scoreManager;
        private Coroutine plantEffectCoroutine;

        public TurnController(Func<bool> readyToPlay) => ReadyToPlay = readyToPlay;

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

        private void Start()
        {
            UpdateMoneyGoal();
            _scoreManager.ResetMoneys();
            if (ReadyToPlay != null)
                StartCoroutine(BeginTurnSequence());
        }

        private void UpdateMoneyGoal()
        {
            moneyGoal = level switch
            {
                0 => 50,
                1 => 100,
                _ => moneyGoal
            };
        }

        // ReSharper disable Unity.PerformanceAnalysis
        // ReSharper disable once MemberCanBePrivate.Global
        public IEnumerator BeginTurnSequence()
        {
            yield return new WaitForEndOfFrame();
            if (ReadyToPlay != null)
                yield return new WaitUntil(ReadyToPlay);
            if (lostGameObjects.activeInHierarchy) lostGameObjects.SetActive(false);
            canClickEnd = false;
            currentTurn = 1;
            if (totalTurns is 0) totalTurns = 1;
            currentRound++;

            yield return new WaitForSeconds(2f);
            try
            {
                if (level == 0 && CardGameMaster.Instance.isSequencingEnabled) _deckManager.PlaceTutorialPlants();
                else _deckManager.PlacePlants();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            yield return new WaitForSeconds(1f);
            try
            {
                if (level == 0  && CardGameMaster.Instance.isSequencingEnabled)
                {
                    _deckManager.DrawTutorialAfflictions();
                    TryPlayQueuedEffects();
                    _deckManager.DrawTutorialActionHand();
                }
                else
                {
                    _deckManager.DrawAfflictions();
                    TryPlayQueuedEffects();
                    _deckManager.DrawActionHand();
                }
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
                CardGameMaster.Instance.turnText!.text = "Turn: " + currentTurn + "/" + turnCount;
            if (CardGameMaster.Instance.roundText)
                CardGameMaster.Instance.roundText!.text = "Round: " + currentRound;
            if (CardGameMaster.Instance.levelText)
                CardGameMaster.Instance.levelText!.text = "Level: " + (level + 1);
        }

        /// <summary>
        ///     Ends the current turn, applies treatments to plants, checks for affliction spread, and updates game state.
        /// </summary>
        /// <remarks>
        ///     This method performs the following operations:
        ///     1. Checks if the action display is still updating or if ending the turn is not allowed. If either is true,
        ///     the method returns early.
        ///     2. Verify if a new round is ready. If so, start the next round and return.
        ///     3. Retrieve all active plant controllers from defined plant locations.
        ///     4. For each plant controller:
        ///     a. Applies all queued treatments.
        ///     B. Flags the associated shaders for an update.
        ///     5. End the current round early if all plants are free of afflictions.
        ///     6. If the maximum turn count has not been reached:
        ///     a. Increments the turn counter.
        ///     B. Spreads afflictions to neighboring plants based on randomized probabilities and valid neighbors.
        ///     C. Draws an action hand for the next turn.
        ///     7. End the current round if the maximum turn count is reached.
        /// </remarks>
        /// <exception cref="System.NullReferenceException">
        ///     Thrown if `deckManager`, `plantLocations`, or any dependencies (e.g., `PlantController` or `PlantCardFunctions`)
        ///     are not properly initialized.
        /// </exception>
        public void EndTurn()
        {
            if (_deckManager.updatingActionDisplay || !canClickEnd) return;

            if (ScoreManager.GetMoneys() >= moneyGoal)
            {
                currentTurn++;
                totalTurns++;
                shopQueued = true; // We'll need to maybe find a use for this
                EndLevel();
                return;
            }

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
                if (debugging) Debug.Log($"Found {plantControllers.Length} PlantControllers in PlantLocation.");

                // Apply queued treatments and update shaders
                foreach (var controller in plantControllers)
                {
                    controller.plantCardFunctions.ApplyQueuedTreatments();
                    controller.ProcessDay();
                    StartCoroutine(PauseRoutine());
                    controller.FlagShadersUpdate();
                }

                TryPlayQueuedEffects();
                var retainedCardHolder = FindFirstObjectByType<RetainedCardHolder>();
                retainedCardHolder.isCardLocked = false;
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
                totalTurns++;
                SpreadAfflictions(plantControllers);
                _deckManager.DrawActionHand();
            }
            else
            {
                StartCoroutine(EndRound());
            }

            _scoreManager.CalculateTreatmentCost();
        }

        /// <summary>
        ///     Spreads afflictions to neighboring plants based on randomized probabilities and valid neighbors.
        /// </summary>
        /// <param name="plantControllers">Array of plant controllers representing the current arrangement of plants.</param>
        /// <remarks>
        ///     This method iterates through each plant controller in the provided array. For each plant, it checks if there are
        ///     any
        ///     active afflictions and if a 50% chance condition is met. If both conditions are satisfied, it selects a random
        ///     neighbor
        ///     (left or right) that does not currently have the same affliction and spreads the affliction to that neighbor.
        ///     If debugging is enabled, it logs the spread of each affliction.
        /// </remarks>
        /// <exception cref="System.NullReferenceException">
        ///     Thrown if any plant controller in the array or their associated properties (e.g., `CurrentAfflictions`) are not
        ///     properly initialized.
        /// </exception>
        private void SpreadAfflictions(PlantController[] plantControllers)
        {
            var random = new Random();
            for (var i = 0; i < plantControllers.Length; i++)
            {
                var controller = plantControllers[i];

                // Skip if no afflictions or 50% chance
                if (!controller.CurrentAfflictions.Any() || random.NextDouble() >= 0.5) continue;

                var affliction = controller.CurrentAfflictions.First();

                if (affliction is PlantAfflictions.ThripsAffliction)
                {
                    // Spread ThripsAffliction to any plant that doesn't have it
                    var targets = plantControllers.Where(p => !p.HasAffliction(affliction)).ToList();
                    if (targets.Count == 0) continue;

                    var target = targets[random.Next(targets.Count)];

                    // Don't spread to something treated with Panacea, or that had the affliction before
                    if (target.UsedTreatments.Any(t => t is PlantAfflictions.Panacea) ||
                        target.HasHadAffliction(affliction))
                        continue;

                    target.AddAffliction(affliction);
                    _scoreManager.CalculateTreatmentCost();
                    StartCoroutine(PauseRoutine());
                    target.FlagShadersUpdate();

                    if (debugging)
                        Debug.Log($"ThripsAffliction spread from {controller.name} to {target.name}.");
                }
                else
                {
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
                        if (!target.UsedTreatments.Any(treatment =>
                                treatment is PlantAfflictions.Panacea || !target.HasHadAffliction(affliction)))
                        {
                            target.AddAffliction(affliction.Clone());
                            _scoreManager.CalculateTreatmentCost();
                        }

                        StartCoroutine(PauseRoutine());
                        target.FlagShadersUpdate();
                    }
                }

                if (debugging)
                    Debug.Log(
                        $"Affliction {affliction} spread from {controller.name} to {plantControllers[i].name}.");
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
            totalTurns++;
            var ppVol = CardGameMaster.Instance.postProcessVolume.gameObject;
            if (ppVol) ppVol.SetActive(false);
            if (debugging) Debug.Log($"Treatment Cost: {_scoreManager.treatmentCost}");
            
            TryPlayQueuedEffects();

            _deckManager.ClearActionHand();
            _scoreManager.CalculateTreatmentCost();

            yield return new WaitForSeconds(delayTime);

            var score = _scoreManager.CalculateScore();
            if (_scoreManager) _scoreManager.treatmentCost = 0;

            _deckManager.ClearAllPlants();

            if (debugging) Debug.Log("Score: " + score);

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

        private void EndLevel()
        {
            level++;
            UpdateMoneyGoal();
            if (!shopQueued) return;
            CardGameMaster.Instance.shopManager.OpenShop();
            shopQueued = false;
        }

        public static void QueuePlantEffect(PlantController plant, ParticleSystem particle = null, AudioClip sound = null,
            float delay = 0.3f)
        {
            PlantEffectQueue.Enqueue(new PlantEffectRequest(plant, particle, sound, delay));
        }

        private void TryPlayQueuedEffects()
        {
            if (plantEffectCoroutine == null && PlantEffectQueue.Count > 0)
                plantEffectCoroutine = StartCoroutine(PlayQueuedPlantEffects());
        }

        private IEnumerator PlayQueuedPlantEffects()
        {
            while (PlantEffectQueue.Count > 0)
            {
                var request = PlantEffectQueue.Dequeue();
                if (request.Plant)
                {
                    if (request.Particle)
                        request.Particle.Play();

                    if (request.Sound && request.Plant.audioSource)
                    {
                        request.Plant.audioSource.pitch = 1f;
                        request.Plant.audioSource.volume = 1f;
                        request.Plant.audioSource.spatialBlend = 0f;
                        request.Plant.audioSource.PlayOneShot(request.Sound);
                    }
                }

                yield return new WaitForSeconds(request.Delay);
            }

            plantEffectCoroutine = null;
        }

        public void ShowBetaScreen()
        {
            winScreen.gameObject.SetActive(true);
            canClickEnd = false;
            CardGameMaster.Instance.eventSystem.GetComponent<InputSystemUIInputModule>().enabled = true;
            winScreen.gameObject.GetComponentInChildren<TextMeshProUGUI>().text =
                "Good job! You beat the first 2 levels in " + currentRound + " rounds and " + totalTurns +
                " turns. That's [excellent!  / pretty good / average / " +
                "... well, it's something. Maybe play it again and see if you can do better!] " +
                "This game is still in development, so check back in for new levels." +
                " If you're interested in Integrated Pest Management as a subject," +
                " or in helping us develop the game further," +
                " sign up for Unity Environmental University's Integrated Pest Management course. " +
                "We're inviting students to help make this game as part of their schoolwork." +
                " Thank you for playing; we hope to see you again soon!";
        }

        public void ResetGame()
        {
            currentRound = 0;
            StartCoroutine(BeginTurnSequence());
            _scoreManager.ResetMoneys();
        }

        private void GameLost() { lostGameObjects.SetActive(true); }
    }
}