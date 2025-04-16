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
        public bool debugging;
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
            // DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(BeginTurnSequence());
        } // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator BeginTurnSequence()
        {
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
        }

        private void Update()
        {
            turnText.text = "Turn: " + currentTurn;
        }

        public void EndTurn()
        {
            if (deckManager.updatingActionDisplay) return;

            // Get PlantControllers in PlantLocation.
            var plantControllers = deckManager.plantLocations
                .SelectMany(location => location.GetComponentsInChildren<PlantController>(false))
                .ToArray();
            if (debugging) Debug.Log($"Found {plantControllers.Length} PlantControllers in PlantLocation.");

            // Cure queued afflictions
            foreach (var controller in plantControllers)
            {
                controller.plantCardFunctions.ApplyQueuedTreatments();
                controller.FlagShadersUpdate();
            }

            if (plantControllers.All(controller => !controller.CurrentAfflictions.Any()))
            {
                EndRound();
                return;
            }

            if (currentTurn != turnCount)
            {
                currentTurn++;

                // TODO Review this spread logic

                // Randomly check if a plant spreads an affliction from CurrentAfflictions to another plant controller
                var random = new Random();

                for (var i = 0; i < plantControllers.Length; i++)
                {
                    var controller = plantControllers[i];
                    if (!controller.CurrentAfflictions.Any() || random.NextDouble() >= 0.5) continue; // 50% chance

                    var affliction = controller.CurrentAfflictions.First();

                    //collect right/left neighbors
                    var neighborOptions = new List<PlantController>();

                    if (i > 0)
                    {
                        var leftNeighbor = plantControllers[i - 1];
                        if (leftNeighbor && !leftNeighbor.HasAffliction(affliction)) neighborOptions.Add(leftNeighbor);
                    }

                    if (i < plantControllers.Length - 1)
                    {
                        var rightNeighbor = plantControllers[i + 1];
                        if (rightNeighbor && !rightNeighbor.HasAffliction(affliction))
                            neighborOptions.Add(rightNeighbor);
                    }

                    // Spread to one neighbor at random
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
                EndRound();
            }
        }

        private void EndRound()
        {
            currentTurn = 0;
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
        }
    }
}