using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;

namespace _project.Scripts.PlayModeTest.Utilities.Reflection
{
    /// <summary>
    ///     Provides cached reflection access to DeckManager's private fields.
    ///     Use this instead of inline reflection to improve test performance and reduce code duplication.
    /// </summary>
    public static class DeckManagerReflection
    {
        private static readonly FieldInfo ActionDeckField;
        private static readonly FieldInfo SideDeckField;
        private static readonly FieldInfo ActionDiscardPileField;
        private static readonly FieldInfo ActionHandField;
        private static readonly MethodInfo AddCardToSideDeckMethod;
        private static readonly MethodInfo AddCardToActionDeckMethod;

        static DeckManagerReflection()
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            ActionDeckField = typeof(DeckManager).GetField("_actionDeck", flags);
            SideDeckField = typeof(DeckManager).GetField("_sideDeck", flags);
            ActionDiscardPileField = typeof(DeckManager).GetField("_actionDiscardPile", flags);
            ActionHandField = typeof(DeckManager).GetField("_actionHand", flags);
            AddCardToSideDeckMethod = typeof(DeckManager).GetMethod("AddCardToSideDeck", flags);
            AddCardToActionDeckMethod = typeof(DeckManager).GetMethod("AddCardToActionDeck", flags);
        }

        public static List<ICard> GetActionDeck(DeckManager dm)
        {
            return ActionDeckField?.GetValue(dm) as List<ICard>;
        }

        public static List<ICard> GetSideDeck(DeckManager dm)
        {
            return SideDeckField?.GetValue(dm) as List<ICard>;
        }

        public static List<ICard> GetDiscardPile(DeckManager dm)
        {
            return ActionDiscardPileField?.GetValue(dm) as List<ICard>;
        }

        public static List<ICard> GetActionHand(DeckManager dm)
        {
            return ActionHandField?.GetValue(dm) as List<ICard>;
        }

        /// <summary>
        ///     Invokes the private AddCardToSideDeck method.
        /// </summary>
        public static void InvokeAddCardToSideDeck(DeckManager dm, List<ICard> sourceDeck, ICard card)
        {
            AddCardToSideDeckMethod?.Invoke(dm, new object[] { sourceDeck, card });
        }

        /// <summary>
        ///     Invokes the private AddCardToActionDeck method.
        /// </summary>
        public static void InvokeAddCardToActionDeck(DeckManager dm, List<ICard> sourceDeck, ICard card)
        {
            AddCardToActionDeckMethod?.Invoke(dm, new object[] { sourceDeck, card });
        }
    }
}