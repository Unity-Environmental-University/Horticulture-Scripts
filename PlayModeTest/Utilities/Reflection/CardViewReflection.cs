using System.Reflection;
using _project.Scripts.Card_Core;
using _project.Scripts.Classes;

namespace _project.Scripts.PlayModeTest.Utilities.Reflection
{
    /// <summary>
    ///     Provides cached reflection access to CardView's private fields.
    /// </summary>
    public static class CardViewReflection
    {
        private static readonly FieldInfo OriginalCardField;

        static CardViewReflection()
        {
            OriginalCardField = typeof(CardView).GetField(
                "_originalCard",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
        }

        /// <summary>
        ///     Sets the private _originalCard field on a CardView instance.
        /// </summary>
        public static void SetOriginalCard(CardView view, ICard card)
        {
            OriginalCardField?.SetValue(view, card);
        }

        /// <summary>
        ///     Gets the private _originalCard field from a CardView instance.
        /// </summary>
        public static ICard GetOriginalCard(CardView view)
        {
            return OriginalCardField?.GetValue(view) as ICard;
        }
    }
}