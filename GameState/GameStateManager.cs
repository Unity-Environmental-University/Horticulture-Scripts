using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.ModLoading;
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
                sideDeck = dm.GetSideDeck().Select(SerializeCard).ToList(),
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

            // Environment Upgrades
            var upgradeManager = CardGameMaster.Instance?.environmentUpgradeManager;
            if (upgradeManager != null)
            {
                data.environmentUpgrades = new EnvironmentUpgradeData
                {
                    activeUpgradeTypeNames = upgradeManager.SerializeUpgrades()
                };
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
                if (data.deckData.sideDeck != null)
                    dm.RestoreSideDeck(data.deckData.sideDeck);
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
                if (restoredCard != null)
                {
                    retained.HeldCard = restoredCard;
                    retained.hasPaidForCard = data.retainedCard.hasPaidForCard;
                    retained.isCardLocked = data.retainedCard.isCardLocked;

                    // Create the visual representation of the retained card
                    retained.RestoreCardVisual();
                }
                else
                {
                    Debug.LogWarning("Retained card could not be restored; clearing retained slot.");
                    retained.ClearHeldCard();
                }
            }
            else
            {
                // No retained card in save data, ensure the slot is clear
                retained.ClearHeldCard();
            }

            // Restore Environment Upgrades
            var upgradeManager = CardGameMaster.Instance?.environmentUpgradeManager;
            if (upgradeManager != null && data.environmentUpgrades?.activeUpgradeTypeNames != null)
            {
                upgradeManager.RestoreUpgrades(data.environmentUpgrades.activeUpgradeTypeNames);
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

        public static CardData SerializeCard(ICard card)
        {
            // Unwrap FoilCard so we save the inner card's type; isFoil flag preserves the foil state
            var inner = card is FoilCard foilCard ? foilCard.Inner : card;
            return new CardData
            {
                cardTypeName = inner.GetType().Name,
                cardTypeFullName = inner.GetType().FullName,
                cardName = inner.Name,
                value = inner.Value,
                baseValue = inner is IPlantCard plantCard ? plantCard.BaseValue : null,
                stickers = inner.Stickers?.Select(SerializeSticker).ToList() ?? new List<StickerData>(),
                isFoil = card.IsFoil
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
            if (!dm || dm.plantLocations == null || dm.plantLocations.Count == 0)
                return new List<PlantData>();

            var list = new List<PlantData>();
            for (var i = 0; i < dm.plantLocations.Count; i++)
            {
                var holder = dm.plantLocations[i];
                if (holder == null || !holder.Transform) continue;

                var plant = holder.Transform.GetComponentInChildren<PlantController>();
                if (!plant) continue;

                list.Add(new PlantData
                {
                    plantCard = SerializeCard(plant.PlantCard),
                    locationIndex = i,
                    currentAfflictions = plant.cAfflictions,
                    priorAfflictions = plant.pAfflictions,
                    usedTreatments = plant.uTreatments,
                    currentTreatments = plant.cTreatments,
                    moldIntensity = plant.moldIntensity,
                    uLocationCards = plant.uLocationCards?.ToList() ?? new List<string>(),
                    infectData = SerializeInfectLevel(plant.PlantCard as IPlantCard),
                    canSpreadAfflictions = plant.canSpreadAfflictions,
                    canReceiveAfflictions = plant.canReceiveAfflictions
                });
            }

            return list;
        }

        /// <summary>
        ///     Serializes the InfectLevel dictionary to a list format compatible with Unity's JsonUtility.
        ///     Only includes non-zero entries to optimize save file size.
        /// </summary>
        internal static List<InfectDataEntry> SerializeInfectLevel(IPlantCard plantCard)
        {
            if (plantCard?.Infect == null)
                return new List<InfectDataEntry>();

            var allInfections = plantCard.Infect.All;
            if (allInfections != null)
                return (from kvp in allInfections
                        where kvp.Value.infect > 0 || kvp.Value.eggs > 0
                        select new InfectDataEntry
                            { source = kvp.Key, infect = kvp.Value.infect, eggs = kvp.Value.eggs })
                    .ToList();
            Debug.LogWarning("InfectLevel.All returned null, skipping infection serialization");
            return new List<InfectDataEntry>();
        }

        /// <summary>
        ///     Reconstructs a card instance from serialized data using its type name.
        /// </summary>
        public static ICard DeserializeCard(CardData data)
        {
            try
            {
                if (data == null)
                {
                    Debug.LogWarning("Could not deserialize card: null data");
                    return null;
                }

                var cardType = ResolveCardType(data.cardTypeFullName, data.cardTypeName, data.cardName);
                if (cardType == null)
                {
                    Debug.LogWarning(
                        $"Unknown card type '{data.cardTypeName ?? "<null>"}' (full: '{data.cardTypeFullName ?? "<null>"}', name: '{data.cardName ?? "<null>"}')");
                    return null;
                }

                if (Activator.CreateInstance(cardType) is not ICard clone)
                {
                    Debug.LogWarning($"Could not create card instance for type: {cardType.FullName}");
                    return null;
                }

                if (data.value.HasValue)
                    clone.Value = data.value.Value;

                // Restore BaseValue for plant cards
                if (clone is IPlantCard plantCard && data.baseValue.HasValue)
                    plantCard.BaseValue = data.baseValue.Value;

                // Restore stickers
                if (data.stickers != null)
                    foreach (var sticker in data.stickers.Select(DeserializeSticker).Where(sticker => sticker != null))
                        clone.ApplySticker(sticker);

                // Wrap in FoilCard if the saved card was a foil variant but isn't natively foil.
                // Guard: sub-interface cards cannot be foiled (FoilCard doesn't forward those interfaces).
                // Cards that override IsFoil directly (e.g. Panacea) don't need the decorator.
                if (data.isFoil && !clone.IsFoil
                    && clone is not (IPlantCard or ILocationCard or IAfflictionCard or IFieldSpell))
                    return new FoilCard(clone);

                return clone;
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not deserialize card '{data?.cardTypeName ?? "<null>"}': {e.Message}");
                return null;
            }
        }

        private static Type ResolveCardType(string fullName, string shortName, string cardName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (!string.IsNullOrWhiteSpace(fullName))
                foreach (var assembly in assemblies)
                {
                    var type = assembly.GetType(fullName, false);
                    if (type != null && typeof(ICard).IsAssignableFrom(type))
                        return type;
                }

            if (!string.IsNullOrWhiteSpace(shortName))
                foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
                    if (type.Name == shortName && typeof(ICard).IsAssignableFrom(type))
                        return type;

            if (string.IsNullOrWhiteSpace(cardName)) return null;
            return !string.Equals(shortName, nameof(RuntimeCard), StringComparison.OrdinalIgnoreCase)
                ? null
                : typeof(RuntimeCard);
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
