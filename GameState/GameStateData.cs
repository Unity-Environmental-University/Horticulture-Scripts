using System;
using System.Collections.Generic;
using _project.Scripts.Core;

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
        
        public List<CardData> afflictionDeck;
        public List<CardData> afflictionHand;
        
        public List<CardData> plantDeck;
        public List<CardData> plantHand;
    }
    
    [Serializable]
    public class PlantData
    {
        public PlantType plantType;
        public CardData plantCard;
        public int locationIndex;
        
        public List<string> currentAfflictions;
        public List<string> priorAfflictions;
        
        public List<string> currentTreatments;
        public List<string> usedTreatments;

        public float moldIntensity;
    }

    [Serializable]
    public class CardData
    {
        public string cardTypeName;
        public int? Value;
        public List<StickerData> stickers;
    }
    
    [Serializable]
    public class StickerData
    {
        public string stickerTypeName;
        public string name;
        public string description;
        public int? Value;
    }
    
    [Serializable]
    public class RetainedCardData
    {
        public CardData card;
        public bool hasPaidForCard;
        public bool isCardLocked;
    }
}
