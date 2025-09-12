using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Cinematics;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.GameState;
using TMPro;
using Unity.Serialization;
using UnityEngine;
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
        public int currentTutorialTurn;
        public int totalTurns;
        public int currentRound;
        public bool canClickEnd;
        public bool newRoundReady;
        public bool debugging;
        public bool shopQueued;
        [DontSerialize] public bool tutorialCompleted;

        public Func<bool> readyToPlay;
        private static readonly Queue<PlantEffectRequest> PlantEffectQueue = new(); 
        private DeckManager _deckManager;
        private ScoreManager _scoreManager;
        private Coroutine plantEffectCoroutine;
        private const int TutorialTurnCount = 5;
        private const int TutorialMoneyGoal = 500;

        public TurnController(Func<bool> readyToPlay) => this.readyToPlay = readyToPlay;

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
            if (readyToPlay != null)
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
            if (readyToPlay != null)
                yield return new WaitUntil(readyToPlay);
            if (lostGameObjects.activeInHierarchy) lostGameObjects.SetActive(false);
            canClickEnd = false;
            currentTurn = 1;
            if (totalTurns is 0) totalTurns = 1;
            currentRound++;

            yield return new WaitForSeconds(2f);

            if (level == 0 && CardGameMaster.IsSequencingEnabled && currentTutorialTurn >= TutorialTurnCount && !tutorialCompleted)
            {
                if (debugging)
                    Debug.Log("[TurnController] Tutorial complete! Transitioning to the regular game...");
                CardGameMaster.Instance.popUpController.ActivatePopUpPanel(null, "Tutorial Complete! Press Continue to proceed to the regular game...");
                yield return new WaitForSeconds(2f);
                _scoreManager.ResetMoneys();
                currentRound = 1;
                tutorialCompleted = true;
            }
            
            if (level == 0 && CardGameMaster.IsSequencingEnabled &&
                currentTutorialTurn < TutorialTurnCount)
            {
                if (debugging)
                    Debug.Log(
                        $"[TurnController] Tutorial: PlaceTutorialPlants (turn {currentTutorialTurn + 1}/{TutorialTurnCount})");
                moneyGoal = TutorialMoneyGoal;
                yield return StartCoroutine(_deckManager.PlaceTutorialPlants());
            }
            else
            {
                if (debugging) Debug.Log("[TurnController] Regular: PlacePlants");
                UpdateMoneyGoal();
                yield return StartCoroutine(_deckManager.PlacePlants());
            }

            if (level == 0 && CardGameMaster.IsSequencingEnabled &&
                currentTutorialTurn < TutorialTurnCount)
            {
                if (debugging)
                    Debug.Log(
                        $"[TurnController] Tutorial: DrawTutorialAfflictions/Action (turn {currentTutorialTurn + 1}/{TutorialTurnCount})");
                _deckManager.DrawTutorialAfflictions();
                TryPlayQueuedEffects();
                if (currentTutorialTurn == 0)
                {
                    CinematicDirector.PlayScene(CardGameMaster.Instance.cinematicDirector.aphidsTimeline);
                    yield return new WaitUntil(readyToPlay);
                }
                // Reveal UI, wait for pop-in to finish, then draw the hand
                var sequencer = FindFirstObjectByType<RobotCardGameSequencer>(FindObjectsInactive.Exclude);
                if (sequencer) yield return StartCoroutine(sequencer.ResumeUIPopInAndWait());
                _deckManager.DrawTutorialActionHand();
            }
            else
            {
                if (debugging) Debug.Log("[TurnController] Regular: DrawAfflictions/Action");
                _deckManager.DrawAfflictions();
                TryPlayQueuedEffects();
                // Reveal UI, wait for pop-in to finish, then draw the hand
                var sequencer = FindFirstObjectByType<RobotCardGameSequencer>(FindObjectsInactive.Exclude);
                if (sequencer) yield return StartCoroutine(sequencer.ResumeUIPopInAndWait());
                _deckManager.DrawActionHand();
            }


            yield return new WaitForSeconds(.5f);
            canClickEnd = true;
        }

        private void Update()
        {
            if (CardGameMaster.Instance.turnText)
                // No hard turn cap; show current turn only
                CardGameMaster.Instance.turnText!.text = "Turn: " + currentTurn;
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
            // Debounce apply button immediately to prevent double-trigger within a single input frame
            canClickEnd = false;

            // If we're ready for a new round, call setup and return immediately
            if (newRoundReady)
            {
                newRoundReady = false;
                StartCoroutine(BeginTurnSequence());
                return;
            }

            // Snapshot plant controllers for this turn's processing
            var plantControllers = _deckManager.plantLocations
                .SelectMany(location => location.GetComponentsInChildren<PlantController>(false))
                .ToArray();

            var cardHolders = CardGameMaster.Instance?.cardHolders;
            if (cardHolders != null)
            {
                if (debugging) Debug.Log($"Found {plantControllers.Length} PlantControllers in PlantLocation.");

                // Apply queued treatments, process day effects, and update visuals for each plant
                foreach (var controller in plantControllers)
                {
                    controller.plantCardFunctions.ApplyQueuedTreatments();
                    controller.ProcessDay();
                    StartCoroutine(PauseRoutine());
                    controller.FlagShadersUpdate();
                }

                // Process location card effects for each spot
                var spotDataHolders = FindObjectsByType<SpotDataHolder>(FindObjectsSortMode.None);
                foreach (var spotDataHolder in spotDataHolders)
                {
                    spotDataHolder.ProcessTurn();
                }

                TryPlayQueuedEffects();
                var retainedCardHolder = FindFirstObjectByType<RetainedCardHolder>();
                if (retainedCardHolder) retainedCardHolder.isCardLocked = false;
            }

            // Re-evaluate health AFTER daily processing to decide on early round end or level end
            // Important: avoid vacuous truth on empty plant list (All() on empty => true)
            var hasPlants = plantControllers.Length > 0;
            var allPlantsHealthy = hasPlants && plantControllers.All(controller =>
                controller.PlantCard is { Value: <= 0 } ||
                (controller.GetInfectLevel() == 0 && controller.GetEggLevel() == 0));

            if (debugging)
            {
                foreach (var controller in plantControllers)
                {
                    var isDead = controller.PlantCard is { Value: <= 0 };
                    var isHealthy = controller.GetInfectLevel() == 0 && controller.GetEggLevel() == 0;
                    Debug.Log($"Plant {controller.name}: Dead={isDead}, Healthy={isHealthy}, Infect={controller.GetInfectLevel()}, Eggs={controller.GetEggLevel()}, Afflictions={controller.CurrentAfflictions.Count}");
                }
                Debug.Log($"PlantControllers count={plantControllers.Length}, All plants healthy (post-process)={allPlantsHealthy}, Money={ScoreManager.GetMoneys()}/{moneyGoal}");
            }

            // During tutorial steps, money goal cannot end the level
            // Money goal can only end level if all plants are also healthy
            if (!(level == 0 && CardGameMaster.IsSequencingEnabled && currentTutorialTurn < TutorialTurnCount)
                && ScoreManager.GetMoneys() >= moneyGoal && allPlantsHealthy)
            {
                if (debugging) Debug.Log("Ending level - money goal reached AND all plants healthy");
                currentTurn++;
                totalTurns++;
                shopQueued = true; // We'll need to maybe find a use for this
                EndLevel();
                return;
            }

            // If all plants are healthy/dead after processing, end the round early
            if (allPlantsHealthy)
            {
                if (debugging) Debug.Log("Ending round early - all plants are dead or healthy after processing");
                StartCoroutine(EndRound(advanceTutorial: true));
                return;
            }

            // Otherwise, continue with normal turn flow (no hard end on turn limit)
            currentTurn++;
            totalTurns++;
            SpreadAfflictions(plantControllers);
            _deckManager.DrawActionHand();
            // Re-enable after scheduling next hand; UI stays disabled while updatingActionDisplay is true
            canClickEnd = true;

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

                var afflictions = controller.CurrentAfflictions;
                var count = afflictions.Count;
                var startIndex = random.Next(count);
                var didSpread = false;

                // Try up to two afflictions (random pick and simple fallback) to reduce bias
                for (var attempt = 0; attempt < Mathf.Min(2, count); attempt++)
                {
                    var affliction = afflictions[(startIndex + attempt) % count];

                    // Build a list of targets
                    List<PlantController> targets;
                    if (affliction is PlantAfflictions.ThripsAffliction)
                    {
                        targets = plantControllers.Where(p => !p.HasAffliction(affliction)).ToList();
                    }
                    else
                    {
                        targets = new List<PlantController>(2);
                        if (i > 0 && !plantControllers[i - 1].HasAffliction(affliction))
                            targets.Add(plantControllers[i - 1]);
                        if (i < plantControllers.Length - 1 && !plantControllers[i + 1].HasAffliction(affliction))
                            targets.Add(plantControllers[i + 1]);
                    }

                    // Eligibility filters
                    targets = targets
                        .Where(t => !t.UsedTreatments.Any(tmt => tmt is PlantAfflictions.Panacea) &&
                                    !t.HasHadAffliction(affliction))
                        .ToList();

                    if (targets.Count == 0) continue;

                    var target = targets[random.Next(targets.Count)];
                    target.AddAffliction(affliction.Clone());
                    _scoreManager.CalculateTreatmentCost();
                    target.FlagShadersUpdate();
                    if (debugging)
                        Debug.Log($"Affliction {affliction} spread from {controller.name} to {target.name}.");
                    didSpread = true;
                    break;
                }

                if (!didSpread && debugging) Debug.Log($"No valid spread targets for {controller.name} this tick.");
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
        private IEnumerator EndRound(float delayTime = 2f, bool advanceTutorial = false)
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

            // If money goal reached at end-of-round, end the level immediately (except during tutorial steps)
            var isTutorialStep = level == 0 && CardGameMaster.IsSequencingEnabled && currentTutorialTurn < TutorialTurnCount;
            if (!isTutorialStep && ScoreManager.GetMoneys() >= moneyGoal)
            {
                shopQueued = true;
                EndLevel();
                yield break;
            }

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
                if (advanceTutorial && level == 0 && CardGameMaster.IsSequencingEnabled &&
                    currentTutorialTurn < TutorialTurnCount)
                    currentTutorialTurn++;
                
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
            if (GameStateManager.SuppressQueuedEffects)
                return;
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
                if (request.plant)
                {
                    if (request.particle)
                        request.particle.Play();

                    if (request.sound && request.plant.audioSource)
                    {
                        request.plant.audioSource.pitch = 1f;
                        request.plant.audioSource.volume = 1f;
                        request.plant.audioSource.spatialBlend = 0f;
                        request.plant.audioSource.PlayOneShot(request.sound);
                    }
                }

                yield return new WaitForSeconds(request.delay);
            }

            plantEffectCoroutine = null;
        }

        public void ClearEffectQueue()
        {
            while (PlantEffectQueue.Count > 0)
            {
                var request = PlantEffectQueue.Dequeue();
                if (request.particle)
                    request.particle.Stop();
            }

            plantEffectCoroutine = null;
        }

        public void ShowBetaScreen()
        {
            winScreen.gameObject.SetActive(true);
            canClickEnd = false;
            CardGameMaster.Instance.uiInputModule.enabled = true;
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
            // Reset tutorial progress when restarting the game
            currentTutorialTurn = 0;
            currentRound = 0;
            tutorialCompleted = false;
            StartCoroutine(BeginTurnSequence());
            _scoreManager.ResetMoneys();
        }

        private void GameLost() { lostGameObjects.SetActive(true); }
    }
}
