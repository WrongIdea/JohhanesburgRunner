namespace JoburgRunner.Environment
{
    /// <summary>
    /// Coarse weather condition for systems that want to react to it (currently
    /// the pigeon spawner: rain thins the flocks, heavy rain clears the ground).
    /// A lightweight global scaffold — set <see cref="Current"/> from whatever
    /// weather/zone driver exists; it defaults to Sunny so behaviour is unchanged
    /// until something drives it.
    /// </summary>
    public enum Weather
    {
        Sunny,
        Overcast,
        Rain,
        HeavyRain
    }

    public static class WeatherState
    {
        public static Weather Current = Weather.Sunny;

        /// <summary>Spawn multiplier for ground flocks; 0 means "no ground flocks".</summary>
        public static float GroundFlockMultiplier => Current switch
        {
            Weather.Rain => 0.4f,       // ~60% fewer
            Weather.HeavyRain => 0f,    // none on the ground
            _ => 1f,
        };

        public static bool AllowsGroundFlocks => Current != Weather.HeavyRain;
    }
}
