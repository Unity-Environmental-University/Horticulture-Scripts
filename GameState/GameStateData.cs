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
        public CardData cardData;
    }

    [Serializable]
    public class TurnData
    {
        
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
    }
    
    [Serializable]
    public class PlantData
    {
        public PlantType plantType;
        public CardData plantCard;
        public int locationIndex;
        
        public List<string> currentAfflictions;
        public List<string> priorAfflictions;
        
        public List<string> usedTreatments;
        public List<string> priorTreatments;

        public float moldIntensity;
        public bool hasThripsFx;
        public bool hasDebuffFX;
    }

    [Serializable]
    public class CardData
    {
    }
}
