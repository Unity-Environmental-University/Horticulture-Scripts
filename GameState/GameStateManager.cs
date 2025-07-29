using System.Linq;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;

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
        }

        public static void LoadGame()
        {
            
        }

        private static CardData SerializeCard(ICard card)
        {
            return new CardData
            {
                CardType = card,
                Value = card.Value
            };
        }
        
    }
}