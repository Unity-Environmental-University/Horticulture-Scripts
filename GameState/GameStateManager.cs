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
            dm.RefreshActionHandDisplay();
            
            // Restore Plants
            CardGameMaster.Instance.StartCoroutine(
                CardGameMaster.Instance.deckManager.RestorePlantsSequentially(data.plants)
            );
            
            // Restore Retained Card
            var retained = Object.FindFirstObjectByType<RetainedCardHolder>();
            if (retained && retained.HeldCard != null) retained.HeldCard = DeserializeCard(data.retainedCard.card);
        }

        private static CardData SerializeCard(ICard card)
        {
            return new CardData
            {
                cardTypeName = card.GetType().Name,
                Value = card.Value
            };
        }

        private static List<PlantData> SerializePlants(DeckManager dm)
        {
            var list = new List<PlantData>();
            for (var i = 0; i < dm.plantLocations.Count; i++)
            {
                var plant = dm.plantLocations[i].GetComponentInChildren<PlantController>();
                if (!plant) continue;

                list.Add(new PlantData
                { 
                    plantType= plant.type,
                    plantCard = SerializeCard(plant.PlantCard),
                    locationIndex = i,
                    currentAfflictions = plant.cAfflictions,
                    priorAfflictions = plant.pAfflictions,
                    usedTreatments = plant.uTreatments,
                    currentTreatments = plant.cTreatments,
                    moldIntensity = plant.moldIntensity
                });
            }
            return list;
        }

        private static void DeserializePlant(PlantData data, DeckManager dm)
        {
            var location = dm.plantLocations[data.locationIndex];
            // Reconstruct plant card to lookup its prefab
            var cardProto = DeserializeCard(data.plantCard);
            var prefab = dm.GetPrefabForCard(cardProto);
            if (prefab == null)
                throw new Exception($"Could not find prefab for plant card type '{cardProto.GetType().Name}'");
            var plantObj = Object.Instantiate(prefab, location.position, location.rotation, location);
            
            var plant = plantObj.GetComponent<PlantController>();
            if (plant == null)
                throw new Exception($"PlantController component missing on instantiated plant prefab '{prefab.name}'");
            // Restore the plant card and any saved state
            plant.PlantCard = DeserializeCard(data.plantCard);
            if (data.currentAfflictions != null)
                foreach (var aff in data.currentAfflictions)
                    plant.AddAffliction(DeckManager.GetAfflictionFromString(aff));
            if (data.usedTreatments != null)
                foreach (var treat in data.usedTreatments)
                    plant.UsedTreatments.Add(DeckManager.GetTreatmentFromString(treat));
            plant.SetMoldIntensity(data.moldIntensity);
        }

        /// <summary>
        /// Reconstructs a card instance from serialized data using its type name.
        /// </summary>
        public static ICard DeserializeCard(CardData data)
        {
            try
            {
                var typeName = data.cardTypeName;
                var cardType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == typeName && typeof(ICard).IsAssignableFrom(t));
                if (cardType == null)
                    throw new Exception($"Unknown card type: {typeName}");
                if (Activator.CreateInstance(cardType) is not ICard clone)
                    throw new Exception($"Could not create card instance for type: {typeName}");
                if (data.Value.HasValue)
                    clone.Value = data.Value.Value;
                return clone;
            }
            catch (Exception e)
            {
                throw new Exception($"Could not deserialize card type {data.cardTypeName}", e);
            }
        }
        
    }
}
