using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.Stickers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _project.Scripts.GameState
{
    public static class GameStateManager
    {
        public static bool SuppressQueuedEffects { get; private set; }

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
                currentRoundInLevel = tc.currentRoundInLevel,
                gameMode = (int)tc.currentGameMode,
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
                actionHand = dm.GetActionHand().Select(SerializeCard).ToList(),
                playerStickers = dm.GetPlayerStickers().Select(SerializeSticker).ToList()
            };

            // Plant Data
            data.plants = SerializePlants(dm);

            // Retained Card
            var retained = Object.FindFirstObjectByType<RetainedCardHolder>();
            if (retained && retained.HeldCard != null)
                data.retainedCard = new RetainedCardData
                {
                    card = SerializeCard(retained.HeldCard),
                    hasPaidForCard = retained.hasPaidForCard,
                    isCardLocked = retained.isCardLocked
                };
            else
                data.retainedCard = null; // No retained card to save

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

            // Validate JSON input before deserialization
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("Invalid game state data: empty or null JSON");
                return;
            }

            // Additional security: limit JSON size to prevent DoS attacks
            if (json.Length > 1024 * 1024) // 1MB limit
            {
                Debug.LogError("Game state data too large, possible security issue");
                return;
            }

            GameStateData data;
            try
            {
                data = JsonUtility.FromJson<GameStateData>(json);
                if (data == null)
                {
                    Debug.LogError("Failed to deserialize game state: null data");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize game state: {e.Message}");
                return;
            }

            // Validate CardGameMaster instance and components exist
            if (CardGameMaster.Instance == null)
            {
                Debug.LogError("CardGameMaster instance not found during load");
                return;
            }

            var tc = CardGameMaster.Instance.turnController;
            var dm = CardGameMaster.Instance.deckManager;

            if (tc == null)
            {
                Debug.LogError("TurnController not found during load");
                return;
            }

            if (dm == null)
            {
                Debug.LogError("DeckManager not found during load");
                return;
            }

            // Restore Turn Data with validation
            if (data.turnData != null)
            {
                tc.turnCount = data.turnData.turnCount;
                tc.level = data.turnData.level;
                tc.moneyGoal = data.turnData.moneyGoal;
                tc.currentTurn = data.turnData.currentTurn;
                tc.currentTutorialTurn = data.turnData.currentTutorialTurn;
                tc.totalTurns = data.turnData.totalTurns;
                tc.currentRound = data.turnData.currentRound;

                // Validate round counter to prevent corrupted save data
                tc.currentRoundInLevel = Mathf.Clamp(data.turnData.currentRoundInLevel, 0, 5);

                // Validate game mode enum
                if (Enum.IsDefined(typeof(GameMode), data.turnData.gameMode))
                {
                    tc.currentGameMode = (GameMode)data.turnData.gameMode;
                }
                else
                {
                    Debug.LogWarning($"Invalid game mode {data.turnData.gameMode}, defaulting to Campaign");
                    tc.currentGameMode = GameMode.Campaign;
                }

                tc.canClickEnd = data.turnData.canClickEnd;
                tc.newRoundReady = data.turnData.newRoundReady;
                tc.shopQueued = data.turnData.shopQueued;
                tc.tutorialCompleted = data.turnData.tutorialCompleted;
            }
            else
            {
                Debug.LogWarning("Turn data is null, skipping turn data restoration");
            }

            // Restore Score with validation
            if (data.scoreData != null)
                ScoreManager.SetScore(data.scoreData.money);
            else
                Debug.LogWarning("Score data is null, skipping score restoration");

            // Restore Decks with validation
            if (data.deckData != null)
            {
                dm.RestoreActionDeck(data.deckData.actionDeck);
                dm.RestoreDiscardPile(data.deckData.discardPile);
                dm.RestoreActionHand(data.deckData.actionHand);
                dm.RefreshActionHandDisplay();
            }
            else
            {
                Debug.LogWarning("Deck data is null, skipping deck restoration");
            }

            // Restore Sticker Inventory
            if (data.deckData!.playerStickers != null)
                dm.RestorePlayerStickers(data.deckData.playerStickers);

            // Suppress any plant effects during restore and clear the queue when done
            SuppressQueuedEffects = true;
            CardGameMaster.Instance.StartCoroutine(RestorePlantsAndClearEffects(data.plants, tc));

            // Restore Retained Card
            var retained = Object.FindFirstObjectByType<RetainedCardHolder>();
            if (retained == null) return;
            if (data.retainedCard is { card: not null })
            {
                // Restore the retained card
                var restoredCard = DeserializeCard(data.retainedCard.card);
                retained.HeldCard = restoredCard;
                retained.hasPaidForCard = data.retainedCard.hasPaidForCard;
                retained.isCardLocked = data.retainedCard.isCardLocked;

                // Create the visual representation of the retained card
                retained.RestoreCardVisual();
            }
            else
            {
                // No retained card in save data, ensure the slot is clear
                retained.ClearHeldCard();
            }
        }

        private static IEnumerator RestorePlantsAndClearEffects(List<PlantData> plantData, TurnController tc)
        {
            yield return CardGameMaster.Instance.deckManager.RestorePlantsSequentially(plantData);
            tc.ClearEffectQueue();
            SuppressQueuedEffects = false;

            // Update plant shaders after restoration to show current afflictions/treatments visually
            var plantControllers = Object.FindObjectsByType<PlantController>(FindObjectsSortMode.None);
            foreach (var plantController in plantControllers) plantController.FlagShadersUpdate();

            // Wait one frame to ensure shader updates are processed
            yield return null;
        }

        private static CardData SerializeCard(ICard card)
        {
            return new CardData
            {
                cardTypeName = card.GetType().Name,
                value = card.Value,
                stickers = card.Stickers?.Select(SerializeSticker).ToList() ?? new List<StickerData>()
            };
        }

        private static StickerData SerializeSticker(ISticker sticker)
        {
            return new StickerData
            {
                stickerTypeName = sticker.GetType().Name,
                name = sticker.Name,
                value = sticker.Value
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

        /// <summary>
        ///     Reconstructs a card instance from serialized data using its type name.
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
                if (data.value.HasValue)
                    clone.Value = data.value.Value;

                // Restore stickers
                if (data.stickers == null) return clone;
                foreach (var sticker in data.stickers.Select(DeserializeSticker).Where(sticker => sticker != null))
                    clone.ApplySticker(sticker);

                return clone;
            }
            catch (Exception e)
            {
                throw new Exception($"Could not deserialize card type {data.cardTypeName}", e);
            }
        }

        public static ISticker DeserializeSticker(StickerData data)
        {
            try
            {
                var typeName = data.stickerTypeName;
                var stickerType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == typeName && typeof(ISticker).IsAssignableFrom(t));

                if (stickerType == null)
                {
                    Debug.LogWarning($"Unknown sticker type: {typeName}");
                    return null;
                }

                ISticker sticker;

                // Handle ScriptableObject-based stickers
                if (typeof(ScriptableObject).IsAssignableFrom(stickerType))
                {
                    // Try to find existing sticker definitions by name first
                    var existing = Resources.FindObjectsOfTypeAll(stickerType)
                        .OfType<ISticker>()
                        .FirstOrDefault(s => s.Name == data.name);
                    if (existing != null)
                        return existing;

                    // If not found, create a runtime instance using ScriptableObject.CreateInstance
                    sticker = ScriptableObject.CreateInstance(stickerType) as ISticker;
                    if (sticker == null)
                    {
                        Debug.LogWarning($"Could not create ScriptableObject instance for type: {typeName}");
                        return null;
                    }
                }
                else
                {
                    // Handle regular classes with Activator.CreateInstance
                    sticker = Activator.CreateInstance(stickerType) as ISticker;
                    if (sticker == null)
                    {
                        Debug.LogWarning($"Could not create sticker instance for type: {typeName}");
                        return null;
                    }
                }

                if (data.value.HasValue)
                    sticker.Value = data.value.Value;

                return sticker;
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not deserialize sticker type {data.stickerTypeName}: {e.Message}");
                return null;
            }
        }
    }
}