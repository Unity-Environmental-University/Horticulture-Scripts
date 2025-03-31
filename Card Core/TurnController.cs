using System;
using System.Collections;
using UnityEngine;

namespace _project.Scripts.Card_Core
{
    public class TurnController : MonoBehaviour
    {
        public ScoreManager scoreManager;
        public DeckManager deckManager;
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
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(BeginTurnSequence());
        }

        private IEnumerator BeginTurnSequence()
        {
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

        public void EndRound()
        {
            deckManager.ClearActionHand();
            var score = scoreManager.CalculateScore();
            Debug.Log("Score: " + score);
        }
    }
}