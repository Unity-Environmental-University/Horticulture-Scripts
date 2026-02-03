using System.Collections.Generic;
using System.Reflection;
using _project.Scripts.Card_Core;
using _project.Scripts.Core;

namespace _project.Scripts.PlayModeTest.Utilities.Reflection
{
    /// <summary>
    ///     Reflection helper for accessing private members of ScoreManager during tests.
    /// </summary>
    public static class ScoreManagerReflection
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

        /// <summary>
        ///     Gets the private static Moneys field value.
        /// </summary>
        public static int GetMoneys(ScoreManager scoreManager)
        {
            var field = typeof(ScoreManager).GetField("Moneys", PrivateStatic);
            return field != null ? (int)field.GetValue(null) : 0;
        }

        /// <summary>
        ///     Sets the private static Moneys field value.
        /// </summary>
        public static void SetMoneys(int value)
        {
            var field = typeof(ScoreManager).GetField("Moneys", PrivateStatic);
            field?.SetValue(null, value);
        }

        /// <summary>
        ///     Gets the private cachedPlants list.
        /// </summary>
        public static List<PlantController> GetCachedPlants(ScoreManager scoreManager)
        {
            var field = typeof(ScoreManager).GetField("cachedPlants", PrivateInstance);
            return field?.GetValue(scoreManager) as List<PlantController>;
        }

        /// <summary>
        ///     Invokes the private CalculateBonuses() method.
        /// </summary>
        public static int InvokeCalculateBonuses(ScoreManager scoreManager)
        {
            var method = typeof(ScoreManager).GetMethod("CalculateBonuses", PrivateInstance);
            return method != null ? (int)method.Invoke(scoreManager, null) : 0;
        }

        /// <summary>
        ///     Invokes the private UpdateCostText() method.
        /// </summary>
        public static void InvokeUpdateCostText(int totalCost)
        {
            var method = typeof(ScoreManager).GetMethod("UpdateCostText", PrivateStatic);
            method?.Invoke(null, new object[] { totalCost });
        }

        /// <summary>
        ///     Invokes the private UpdateProfitText() method.
        /// </summary>
        public static void InvokeUpdateProfitText(int potProfit)
        {
            var method = typeof(ScoreManager).GetMethod("UpdateProfitText", PrivateStatic);
            method?.Invoke(null, new object[] { potProfit });
        }
    }
}