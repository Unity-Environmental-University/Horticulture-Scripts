using System;
using System.Collections.Generic;
using _project.Scripts.Classes;
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
        // Data for the card retained between rounds
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
        // Affliction decks and hands
        public List<CardData> afflictionDeck;
        public List<CardData> afflictionHand;
        // Plant decks and hands
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
        public bool hasThripsFx;
        public bool hasDebuffFx;
    }

    [Serializable]
    public class CardData
    {
        public ICard CardType;
        public int? Value;
    }

    /// <summary>
    /// Data for a card retained between rounds.
    /// </summary>
    [Serializable]
    public class RetainedCardData
    {
        public CardData card;
        public bool hasPaidForCard;
        public bool isCardLocked;
    }
}
