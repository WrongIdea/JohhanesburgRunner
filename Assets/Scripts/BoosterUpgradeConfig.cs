using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Single source of truth for booster duration and upgrade-cost balancing.
    /// Duration index 0 is level 1. Cost index 0 upgrades level 1 to level 2.
    /// </summary>
    public static class BoosterUpgradeConfig
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 10;

        static readonly float[][] Durations =
        {
            new[] { 8f, 12f, 16f, 20f, 24f, 28f, 32f, 36f, 40f, 45f }, // Taxi Magnet
            new[] { 6f, 10f, 14f, 18f, 22f, 26f, 30f, 35f, 40f, 45f }, // Jozi Sneakers
            new[] { 5f, 8f, 11f, 15f, 19f, 24f, 29f, 34f, 39f, 45f },  // Drone Boost
            new[] { 8f, 12f, 16f, 21f, 26f, 31f, 35f, 39f, 42f, 45f }, // Ubuntu Multiplier
            new[] { 6f, 10f, 14f, 18f, 22f, 26f, 30f, 35f, 40f, 45f }, // Hoverboard
            new[] { 8f, 12f, 16f, 20f, 24f, 28f, 32f, 36f, 40f, 45f }, // Double Coins
            new[] { 8f, 12f, 16f, 20f, 24f, 28f, 32f, 36f, 40f, 45f }, // Ubuntu Pulse
        };

        static readonly int[][] UpgradeCosts =
        {
            new[] { 100, 250, 500, 900, 1400, 2100, 3000, 4200, 5600 }, // Taxi Magnet
            new[] { 120, 300, 600, 1000, 1600, 2400, 3400, 4700, 6200 }, // Jozi Sneakers
            new[] { 100, 250, 500, 900, 1500, 2300, 3300, 4600, 6100 },  // Drone Boost
            new[] { 150, 350, 700, 1200, 1900, 2800, 3900, 5200, 6800 }, // Ubuntu Multiplier
            new[] { 120, 300, 600, 1000, 1600, 2400, 3400, 4700, 6200 }, // Hoverboard
            new[] { 150, 350, 700, 1200, 1900, 2800, 3900, 5200, 6800 }, // Double Coins
            new[] { 200, 450, 850, 1400, 2200, 3200, 4500, 6200, 8200 }, // Ubuntu Pulse — the flagship combo item, priced above the rest
        };

        public static float Duration(PowerUpType type, int level)
        {
            return Durations[(int)type][ToIndex(level)];
        }

        public static int UpgradeCost(PowerUpType type, int currentLevel)
        {
            int level = Mathf.Clamp(currentLevel, MinLevel, MaxLevel);
            return level >= MaxLevel ? 0 : UpgradeCosts[(int)type][level - MinLevel];
        }

        public static bool IsMaxLevel(int level)
        {
            return level >= MaxLevel;
        }

        static int ToIndex(int level)
        {
            return Mathf.Clamp(level, MinLevel, MaxLevel) - MinLevel;
        }
    }
}
