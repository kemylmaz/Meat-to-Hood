using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>Calculates a service reward while rounding only the final result.</summary>
    public static class RewardCalculator
    {
        public static int Calculate(int baseAmount, float serviceMultiplier = 1f)
        {
            if (baseAmount <= 0 || serviceMultiplier <= 0f)
                return 0;

            float combinedMultiplier = serviceMultiplier
                * RushHourSystem.IncomeMultiplier
                * ComboSystem.CurrentMultiplier
                * PlayerUpgradeSystem.IncomeMultiplier;

            return Mathf.Max(1, Mathf.RoundToInt(baseAmount * combinedMultiplier));
        }
    }
}
