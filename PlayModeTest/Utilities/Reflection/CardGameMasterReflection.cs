using System.Reflection;
using _project.Scripts.Card_Core;

namespace _project.Scripts.PlayModeTest.Utilities.Reflection
{
    /// <summary>
    ///     Provides cached reflection access to CardGameMaster's private and static members.
    ///     Use this for setting up singleton instances in tests.
    /// </summary>
    public static class CardGameMasterReflection
    {
        private static readonly PropertyInfo InstanceProperty;

        static CardGameMasterReflection()
        {
            InstanceProperty = typeof(CardGameMaster).GetProperty(
                "Instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );
        }

        /// <summary>
        ///     Sets the CardGameMaster singleton instance via reflection.
        /// </summary>
        public static void SetInstance(CardGameMaster instance)
        {
            InstanceProperty?.SetValue(null, instance);
        }

        /// <summary>
        ///     Clears the CardGameMaster singleton instance.
        /// </summary>
        public static void ClearInstance()
        {
            InstanceProperty?.SetValue(null, null);
        }
    }
}