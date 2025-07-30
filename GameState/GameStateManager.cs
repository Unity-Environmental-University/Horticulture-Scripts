using System;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _project.Scripts.GameState
{
    public static class GameStateManager
    {
        public static void SaveGame()
        {
            var data = new GameStateData();
            
            // Turn Data
            var tc = CardGameMaster.Instance.turnController;
            data.turnData = new TurnData
            {
                turnCount = tc.turnCount,
                level = tc.level,
                moneyGoal = tc.moneyGoal,
                currentTurn = tc.currentTurn,
                currentTutorialTurn = tc.currentTutorialTurn,
                totalTurns = tc.totalTurns,
                currentRound = tc.currentRound,
                canClickEnd = tc.canClickEnd,
                newRoundReady = tc.newRoundReady,
                shopQueued = tc.shopQueued,
                tutorialCompleted = tc.tutorialCompleted
            };
            
            // Score Data
            data.scoreData = new ScoreData
            {
                money = ScoreManager.GetMoneys()
            };

            // Deck Data
            var dm = CardGameMaster.Instance.deckManager;
            data.deckData = new DeckData
            {
                actionDeck = dm.GetActionDeck().Select(SerializeCard).ToList(),
                discardPile = dm.GetDiscardPile().Select(SerializeCard).ToList(),
                actionHand = dm.GetActionHand().Select(SerializeCard).ToList()
            };
            
            // Plant Data
            data.plants = SerializePlants(dm);
            
            // Retained Card
            var retained = Object.FindFirstObjectByType<RetainedCardHolder>();
            if (retained && retained.HeldCard != null)
            { 
                data.retainedCard.card = SerializeCard(retained.HeldCard);
                data.retainedCard.hasPaidForCard = retained.hasPaidForCard;
                data.retainedCard.isCardLocked = retained.isCardLocked;
            }
            
            // Save to PlayerPrefs
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("GameState", json);
            PlayerPrefs.Save();
        }

        public static void LoadGame()
        {
            if (!PlayerPrefs.HasKey("GameState"))
            {
                Debug.LogError("Game state not found!");
                return;
            }
            
            var json = PlayerPrefs.GetString("GameState");
            var data = JsonUtility.FromJson<GameStateData>(json);
            
            var tc = CardGameMaster.Instance.turnController;
            var dm = CardGameMaster.Instance.deckManager;
            
            // Restore Turn Data
            tc.turnCount =  data.turnData.turnCount;
            tc.level = data.turnData.level;
            tc.moneyGoal = data.turnData.moneyGoal;
            tc.currentTurn = data.turnData.currentTurn;
            tc.currentTutorialTurn = data.turnData.currentTutorialTurn;
            tc.totalTurns = data.turnData.totalTurns;
            tc.currentRound = data.turnData.currentRound;
            tc.canClickEnd = data.turnData.canClickEnd;
            tc.newRoundReady = data.turnData.newRoundReady;
            tc.shopQueued = data.turnData.shopQueued;
            tc.tutorialCompleted = data.turnData.tutorialCompleted;
            
            // Restore Score
            ScoreManager.SetScore(data.scoreData.money);
            
            // Restore Decks
            dm.RestoreActionDeck(data.deckData.actionDeck);
            dm.RestoreDiscardPile(data.deckData.discardPile);
            dm.RestoreActionHand(data.deckData.actionHand);
            
            // Remove Existing Plants? //TODO REVIEW
            dm.ClearAllPlants();

            foreach (var plantData in data.plants)
                DeserializePlant(plantData, dm);
            
            // Restore Retained Card
            var retained = Object.FindFirstObjectByType<RetainedCardHolder>();
            if (retained && retained.HeldCard != null) retained.HeldCard = DeserializeCard(data.retainedCard.card);
        }

        private static CardData SerializeCard(ICard card)
        {
            return new CardData
            {
                CardType = card,
                Value = card.Value
            };
        }

        private static List<PlantData> SerializePlants(DeckManager dm)
        {
            var list = new List<PlantData>();
            for (var i = 0; i < dm.plantLocations.Count; i++)
            {
                var plant = dm.plantLocations[i].GetComponentInChildren<PlantController>();
                if (plant == null) continue;

                list.Add(new PlantData
                { 
                    plantType= plant.type,
                    plantCard = SerializeCard(plant.PlantCard),
                    locationIndex = i,
                    CurrentAfflictions = plant.CurrentAfflictions,
                    PriorAfflictions = plant.PriorAfflictions,
                    UsedTreatments = plant.UsedTreatments,
                    CurrentTreatments = plant.CurrentTreatments,
                    moldIntensity = plant.moldIntensity
                });
            }
            return list;
        }

        private static void DeserializePlant(PlantData data, DeckManager dm)
        {
            var location = dm.plantLocations[data.locationIndex];
            var prefab = dm.GetPrefabForCard(data.plantCard.CardType);
            var plantObj = Object.Instantiate(prefab, location.position, location.rotation, location);
            
            var plant = plantObj.GetComponent<PlantController>();
            plant.PlantCard = DeserializeCard(data.plantCard);

            foreach (var aff in data.CurrentAfflictions)
                plant.AddAffliction((aff));
            
            foreach (var treat in data.UsedTreatments)
                plant.UsedTreatments.Add(treat);
            
            plant.SetMoldIntensity(data.moldIntensity);
        }

        private static ICard DeserializeCard(CardData data)
        {
            try
            {
                var clone = data.CardType.Clone();
                if (data.Value.HasValue)
                    clone.Value = data.Value.Value;

                return clone;
            }
            catch (Exception e)
            {
                throw new Exception($"Could not deserialize card type {data.CardType}", e);
            }
        }
        
    }
}