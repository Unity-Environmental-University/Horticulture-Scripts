using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Analytics;
using _project.Scripts.Cinematics;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.GameState;
using _project.Scripts.UI;
using TMPro;
using Unity.Serialization;
using UnityEngine;
using Random = System.Random;

namespace _project.Scripts.Card_Core
{
    public enum GameMode
    {
        Tutorial,
        Campaign,
        Endless
    }

    public class TurnController : MonoBehaviour
    {
        public GameObject lostGameObjects;
        public GameObject winScreen;
        public Sprite cardDiagram;
        public int turnCount = 4;
        public int level;
        public int moneyGoal;
        public GameMode currentGameMode = GameMode.Campaign;
        public int currentTurn;
        public int currentTutorialTurn;
        public int totalTurns;
        public int currentRound;
        public int currentRoundInLevel;
        public bool canClickEnd;
        public bool newRoundReady;
        public bool debugging;
        public bool shopQueued;
        [DontSerialize] public bool tutorialCompleted;

        public Func<bool> readyToPlay;
        private static readonly Queue<PlantEffectRequest> PlantEffectQueue = new();
        private static readonly object PlantEffectQueueLock = new();
        private DeckManager _deckManager;
        private ScoreManager _scoreManager;
        private Coroutine plantEffectCoroutine;

        private const int TutorialTurnCount = 5;
        private const int TutorialMoneyGoal = 500;
        private const int RoundsPerLevel = 5;

        /// <summary>
        /// Gets a value indicating whether the current game state represents an active tutorial step.
        /// </summary>
        /// <remarks>
        /// A tutorial step is active when:
        /// - The player is on level 0 (the tutorial level)
        /// - Tutorial sequencing is enabled via CardGameMaster
        /// - The current tutorial turn is less than the total tutorial turn count
        ///
        /// This property is used to determine whether to use tutorial-specific logic for:
        /// - Drawing action cards (tutorial cards vs. regular deck)
        /// - Placing plants (tutorial plants vs. random plants)
        /// - Victory conditions (tutorial bypasses money goals)
        /// - Round progression logic
        /// </remarks>
        /// <value>
        /// <c>true</c> if currently in an active tutorial step; otherwise, <c>false</c>.
        /// </value>
        private bool IsActiveTutorialStep =>
            level == 0 && CardGameMaster.IsSequencingEnabled && currentTutorialTurn < TutorialTurnCount;

        /// <summary>
        /// Gets a value indicating whether the tutorial has just been completed.
        /// </summary>
        /// <remarks>
        /// The tutorial is considered just completed when:
        /// - The player is on level 0 (the tutorial level)
        /// - Tutorial sequencing is enabled via CardGameMaster
        /// - The current tutorial turn has reached or exceeded the total tutorial turn count
        ///
        /// This property is used to trigger the tutorial-to-normal-game transition, which includes:
        /// - Displaying the "Tutorial Complete" message
        /// - Clearing cardholders
        /// - Resetting the money counter
        /// - Resetting the action deck
        /// </remarks>
        /// <value>
        /// <c>true</c> if the tutorial was just completed; otherwise, <c>false</c>.
        /// </value>
        private bool IsTutorialJustCompleted =>
            level == 0 && CardGameMaster.IsSequencingEnabled && currentTutorialTurn >= TutorialTurnCount;

        public TurnController(Func<bool> readyToPlay) => this.readyToPlay = readyToPlay;

        private static TurnController Instance { get; set; }

        private void Awake()
        {
            var master = CardGameMaster.Instance;
            if (!master)
            {
                TryGetComponent(out master);
                if (!master)
                    master = FindFirstObjectByType<CardGameMaster>(FindObjectsInactive.Include);
            }

            if (master)
            {
                _deckManager = master.deckManager ? master.deckManager : master.GetComponent<DeckManager>();
                _scoreManager = master.scoreManager ? master.scoreManager : master.GetComponent<ScoreManager>();
            }

            if (!_deckManager)
                TryGetComponent(out _deckManager);
            if (!_scoreManager)
                TryGetComponent(out _scoreManager);

            if (!_deckManager || !_scoreManager)
                Debug.LogWarning(
                    "[TurnController] Missing deck or score manager references during Awake; running with local fallbacks.");

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
            // Set game mode based on level and tutorial state
            if (level == 0 && CardGameMaster.IsSequencingEnabled)
                currentGameMode = GameMode.Tutorial;
            else
                currentGameMode = GameMode.Campaign; // Explicitly set Campaign for non-tutorial games

            UpdateMoneyGoal();
            _scoreManager.ResetMoneys();
            if (readyToPlay != null)
                StartCoroutine(BeginTurnSequence());
        }

        private void UpdateMoneyGoal()
        {
            moneyGoal = level switch
            {
                0 => 50,        // Tutorial
                1 or 2 => 100,  // Levels 1-2
                _ => 100 + ((level - 2) * 50)  // Levels 3+: $150, $200, $250, etc.
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

            if (IsTutorialJustCompleted && !tutorialCompleted)
            {
                if (debugging)
                    Debug.Log("[TurnController] Tutorial complete! Transitioning to the regular game...");
                CardGameMaster.Instance.popUpController.ActivatePopUpPanel(null,false,
                    "Tutorial Complete! Press Continue to proceed to the regular game...");
                yield return new WaitForSeconds(2f);
                var cardHolders = CardGameMaster.Instance.cardHolders;
                if (cardHolders != null)
                    foreach (var h in cardHolders.Where(holder => holder))
                        h.ClearHolder();
                _scoreManager.ResetMoneys();
                currentRound = 1;
                tutorialCompleted = true;
                // Transition to Campaign mode after tutorial
                currentGameMode = GameMode.Campaign;
                _deckManager.ResetActionDeckAfterTutorial();
            }
            
            if (IsActiveTutorialStep)
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

            // Record round start analytics (after plants are placed for accurate count)
            try
            {
                var plantCount = _deckManager.plantLocations.Count(loc =>
                    loc.GetComponentInChildren<PlantController>(true) != null);

                AnalyticsFunctions.RecordRoundStart(
                    currentRound,
                    plantCount,
                    ScoreManager.GetMoneys(),
                    moneyGoal,
                    IsActiveTutorialStep
                );
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Analytics] RecordRoundStart error: {ex.Message}");
            }

            if (IsActiveTutorialStep)
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
                    CardGameMaster.Instance.popUpController.ActivatePopUpPanel(cardDiagram, true,
                        "Here's a quick outline of how the cards work!");
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

                // Record turn starts analytics before drawing action hand
                try
                {
                    var afflictedCount = _deckManager.plantLocations
                        .Select(loc => loc.GetComponentInChildren<PlantController>(true))
                        .Count(p => p != null && p.CurrentAfflictions.Count > 0);

                    AnalyticsFunctions.RecordTurnStart(
                        currentRound,
                        currentTurn,
                        _deckManager.cardsDrawnPerTurn,
                        ScoreManager.GetMoneys(),
                        afflictedCount
                    );
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Analytics] RecordTurnStart error: {ex.Message}");
                }

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
            {
                // Show progress within level for Campaign mode
                if (currentGameMode == GameMode.Campaign && !IsActiveTutorialStep)
                    CardGameMaster.Instance.roundText!.text = $"Round: {currentRoundInLevel}/{RoundsPerLevel}";
                else
                    CardGameMaster.Instance.roundText!.text = "Round: " + currentRound;
            }
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

            var spotDataHolders = FindObjectsByType<SpotDataHolder>(FindObjectsSortMode.None);

            var cardHolders = CardGameMaster.Instance?.cardHolders;
            if (cardHolders != null)
            {
                if (debugging) Debug.Log($"Found {plantControllers.Length} PlantControllers in PlantLocation.");

                // Apply queued treatments, process day effects, and update visuals for each plant
                foreach (var controller in plantControllers)
                {
                    controller.plantCardFunctions.ApplyQueuedTreatments();
                    controller.ProcessDay();
                    controller.FlagShadersUpdate();
                }

                // Add a single pause coroutine after processing all plants to avoid Race Condition
                StartCoroutine(PauseRoutine());

                // Process location card effects for each spot
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
            // In Campaign mode, level only ends at the end of 5 rounds (not mid-round)
            if (!IsActiveTutorialStep && currentGameMode != GameMode.Campaign &&
                ScoreManager.GetMoneys() >= moneyGoal && allPlantsHealthy)
            {
                if (debugging) Debug.Log("Ending level - money goal reached AND all plants healthy");
                currentTurn++;
                totalTurns++;
                shopQueued = true; // We'll need to maybe find a use for this
                FinalizeLocationCards();
                EndLevel();
                return;
            }

            // If all plants are healthy/dead after processing, end the round early
            if (allPlantsHealthy)
            {
                if (debugging) Debug.Log("Ending round early - all plants are dead or healthy after processing");
                FinalizeLocationCards();
                StartCoroutine(EndRound(advanceTutorial: true));
                return;
            }
            
            var completedTurn = currentTurn;
            
            // Otherwise, continue with normal turn flow (no hard end on turn limit)
            currentTurn++;
            totalTurns++;
            SpreadAfflictions(plantControllers);
            FinalizeLocationCards();

            // Record turn end analytics before counter-advance
            try
            {
                AnalyticsFunctions.RecordTurnEnd(
                    currentRound,
                    completedTurn,
                    ScoreManager.GetMoneys()
                );
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Analytics] RecordTurnEnd error: {ex.Message}");
            }

            if (IsActiveTutorialStep)
            {
                _deckManager.DrawTutorialActionHand();
            }
            else
            {
                _deckManager.DrawActionHand();
            }
            // Re-enable after scheduling next hand; UI stays disabled while updatingActionDisplay is true
            canClickEnd = true;

            _scoreManager.CalculateTreatmentCost();
            return;

            void FinalizeLocationCards()
            {
                foreach (var spotDataHolder in spotDataHolders) spotDataHolder?.FinalizeLocationCardTurn();
            }
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
                if (!controller.CurrentAfflictions.Any()
                    || !controller.canSpreadAfflictions
                    || random.NextDouble() >= 0.5) continue;

                var afflictions = controller.CurrentAfflictions;
                var count = afflictions.Count;
                var startIndex = random.Next(count);
                var didSpread = false;

                // Try up to two afflictions (random pick and simple fallback) to reduce bias
                for (var attempt = 0; attempt < Mathf.Min(2, count); attempt++)
                {
                    var affliction = afflictions[(startIndex + attempt) % count];

                    if (!affliction.IsSpreadable) continue;
                    
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
                                    !t.HasHadAffliction(affliction) && t.canReceiveAfflictions)
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
        /// Prepares the game for the next round by clearing plants, applying queued treatments,
        /// and checking if the game should continue or end.
        /// </summary>
        /// <param name="score">The player's current score after round calculations</param>
        /// <param name="advanceTutorial">Whether to advance the tutorial turn counter</param>
        private void PrepareNextRound(int score, bool advanceTutorial = false)
        {
            _deckManager.ClearAllPlants();

            if (debugging) Debug.Log("Score: " + score);

            var pControllers = _deckManager.plantLocations
                .SelectMany(location => location.GetComponentsInChildren<PlantController>(false))
                .ToArray();

            if (debugging) Debug.Log($"Found {pControllers.Length} PlantControllers in PlantLocation.");

            foreach (var controller in pControllers)
            {
                controller.plantCardFunctions.ApplyQueuedTreatments();
                controller.FlagShadersUpdate();
            }

            if (score > 0)
            {
                if (advanceTutorial && IsActiveTutorialStep)
                    currentTutorialTurn++;

                newRoundReady = true;
                canClickEnd = true;
            }
            else
            {
                GameLost();
            }
        }

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
            var roundTurnCount = currentTurn; // Save turn count before reset
            currentTurn = 0;
            totalTurns++;
            var ppVol = CardGameMaster.Instance.postProcessVolume.gameObject;
            if (ppVol) ppVol.SetActive(false);
            if (debugging) Debug.Log($"Treatment Cost: {_scoreManager.treatmentCost}");
            
            TryPlayQueuedEffects();

            _deckManager.ClearActionHand();
            _scoreManager.CalculateTreatmentCost();

            yield return new WaitForSeconds(delayTime);

            var scoreBeforeRound = ScoreManager.GetMoneys();
            var score = _scoreManager.CalculateScore();
            if (_scoreManager) _scoreManager.treatmentCost = 0;
            var scoreDelta = score - scoreBeforeRound;
            var roundVictory = !IsActiveTutorialStep && ScoreManager.GetMoneys() >= moneyGoal;

            // Count plant health status and record round end analytics
            try
            {
                var plantControllers = _deckManager.plantLocations
                    .Select(loc => loc.GetComponentInChildren<PlantController>(true))
                    .Where(p => p != null)
                    .ToArray();

                var plantsHealthy = plantControllers.Count(p => p.CurrentAfflictions.Count == 0);
                var plantsDead = _deckManager.plantLocations.Count - plantControllers.Length;
                
                AnalyticsFunctions.RecordRoundEnd(
                    currentRound,
                    roundTurnCount,
                    score,
                    scoreDelta,
                    plantsHealthy,
                    plantsDead,
                    scoreDelta > 0,
                    roundVictory
                );
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Analytics] RecordRoundEnd error: {ex.Message}");
            }

            // Handle different game modes
            if (currentGameMode == GameMode.Campaign && !IsActiveTutorialStep)
            {
                // Campaign mode: 5-round system with rent payment
                currentRoundInLevel++;

                if (debugging) Debug.Log($"Campaign: Round {currentRoundInLevel}/{RoundsPerLevel} completed");

                if (currentRoundInLevel >= RoundsPerLevel)
                {
                    // Rent check after 5 rounds
                    var currentMoney = ScoreManager.GetMoneys();
                    if (currentMoney >= moneyGoal)
                    {
                        // Success: pay rent and advance to the next level
                        if (debugging)
                            Debug.Log($"Rent paid: ${moneyGoal}. Remaining money: ${currentMoney - moneyGoal}");
                        currentRoundInLevel = 0; // Reset BEFORE EndLevel to prevent race condition
                        ScoreManager.SubtractMoneys(moneyGoal);
                        shopQueued = true;
                        EndLevel();
                    }
                    else
                    {
                        // Failure: cannot afford rent
                        if (debugging) Debug.Log($"Cannot afford rent. Money: ${currentMoney}, Rent: ${moneyGoal}");
                        GameLost();
                    }

                    yield break;
                }
                
                // Continue to the next round in Campaign mode
                PrepareNextRound(score);
            }
            else
            {
                // Tutorial or Endless mode: Original logic
                // If money goal reached at end-of-round, end the level immediately (except during tutorial steps)
                if (!IsActiveTutorialStep && ScoreManager.GetMoneys() >= moneyGoal)
                {
                    shopQueued = true;
                    EndLevel();
                    yield break;
                }

                PrepareNextRound(score, advanceTutorial);
            }
        }

        private void EndLevel()
        {
            level++;
            UpdateMoneyGoal();
            _deckManager.ResetRedrawCount();
            DeckManager.GeneratePlantPrices();
            if (!shopQueued) return;
            CardGameMaster.Instance.shopManager.OpenShop();
            shopQueued = false;
        }

        public static void QueuePlantEffect(PlantController plant, ParticleSystem particle = null, AudioClip sound = null,
            float delay = 0.3f)
        {
            if (GameStateManager.SuppressQueuedEffects)
                return;

            lock (PlantEffectQueueLock)
            {
                PlantEffectQueue.Enqueue(new PlantEffectRequest(plant, particle, sound, delay));
            }
        }

        private void TryPlayQueuedEffects()
        {
            lock (PlantEffectQueueLock)
            {
                if (plantEffectCoroutine == null && PlantEffectQueue.Count > 0)
                    plantEffectCoroutine = StartCoroutine(PlayQueuedPlantEffects());
            }
        }

        private IEnumerator PlayQueuedPlantEffects()
        {
            while (true)
            {
                PlantEffectRequest request;
                bool hasRequest;

                lock (PlantEffectQueueLock)
                {
                    hasRequest = PlantEffectQueue.Count > 0;
                    request = hasRequest ? PlantEffectQueue.Dequeue() : null;
                }

                if (!hasRequest)
                    break;

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

            lock (PlantEffectQueueLock)
            {
                plantEffectCoroutine = null;
            }
        }

        public void ClearEffectQueue()
        {
            lock (PlantEffectQueueLock)
            {
                while (PlantEffectQueue.Count > 0)
                {
                    var request = PlantEffectQueue.Dequeue();
                    if (request.particle)
                        request.particle.Stop();
                }
            }

            plantEffectCoroutine = null;
        }

        public void ShowBetaScreen()
        {
            winScreen.gameObject.SetActive(true);
            canClickEnd = false;
            UIInputManager.RequestEnable("TurnController");
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
            currentRoundInLevel = 0; // Reset Campaign mode round counter
            tutorialCompleted = false;
            _deckManager.ResetRedrawCount();

            // Reset to appropriate game mode
            if (level == 0 && CardGameMaster.IsSequencingEnabled)
                currentGameMode = GameMode.Tutorial;
            else
                currentGameMode = GameMode.Campaign;

            var holders = CardGameMaster.Instance.cardHolders;
            if (holders != null)
                foreach (var holder in holders.Where(holder => holder))
                    holder.ClearHolder();

            _deckManager.ResetActionDeckAfterTutorial();
            StartCoroutine(BeginTurnSequence());
            _scoreManager.ResetMoneys();
        }

        private void GameLost() { lostGameObjects.SetActive(true); }

        /// <summary>
        /// Clean up coroutines and queues on component destruction to prevent memory leaks
        /// This may not be necessary, except if the player returns to the menu.
        /// </summary>
        private void OnDestroy()
        {
            // Stop and clear plant effect coroutine and queue
            Coroutine coroutineToStop;
            lock (PlantEffectQueueLock)
            {
                coroutineToStop = plantEffectCoroutine;
                plantEffectCoroutine = null;
                PlantEffectQueue.Clear();
            }

            if (coroutineToStop != null) StopCoroutine(coroutineToStop);
        }
    }
}
