using System;
using System.Collections.Generic;

namespace _project.Scripts.GameState
{
    [Serializable]
    public class GameStateData
    {
        public TurnData turnData;
        public ScoreData scoreData;
        public DeckData deckData;
        public List<PlantData> plants;
        public RetainedCardData retainedCard;
    }

    [Serializable]
    public class TurnData
    {
        public int turnCount;
        public int level;
        public int moneyGoal;
        public int currentTurn;
        public int currentTutorialTurn;
        public int totalTurns;
        public int currentRound;
        public int currentRoundInLevel;
        public int gameMode; // 0=Tutorial, 1=Campaign, 2=Endless
        public bool canClickEnd;
        public bool newRoundReady;
        public bool shopQueued;
        public bool tutorialCompleted;
    }

    [Serializable]
    public class ScoreData
    {
        public int money;
    }

    [Serializable]
    public class DeckData
    {
        public List<CardData> actionDeck;
        public List<CardData> discardPile;
        public List<CardData> actionHand;
        public List<CardData> sideDeck;

        public List<CardData> afflictionDeck;
        public List<CardData> afflictionHand;

        public List<CardData> plantDeck;
        public List<CardData> plantHand;

        public List<StickerData> playerStickers;
    }

    [Serializable]
    public class InfectDataEntry
    {
        public string source;
        public int infect;
        public int eggs;
    }

    [Serializable]
    public class PlantData
    {
        public CardData plantCard;
        public int locationIndex;

        public List<string> currentAfflictions;
        public List<string> priorAfflictions;

        public List<string> currentTreatments;
        public List<string> usedTreatments;

        public float moldIntensity;

        public List<string> uLocationCards;
        public List<InfectDataEntry> infectData;
        public bool canSpreadAfflictions;
        public bool canReceiveAfflictions;
    }

    [Serializable]
    public class CardData
    {
        public string cardTypeName;
        public string cardTypeFullName;
        public string cardName;
        public List<StickerData> stickers;
        public int? value;
        public int? baseValue;
    }

    [Serializable]
    public class StickerData
    {
        public string stickerTypeName;
        public string name;
        public int? value;
    }

    [Serializable]
    public class RetainedCardData
    {
        public CardData card;
        public bool hasPaidForCard;
        public bool isCardLocked;
    }
}
