namespace JoburgRunner.Environment.Pigeons
{
    /// <summary>Coarse weather the pigeon system reacts to (Part 9).</summary>
    public enum PigeonWeatherKind { Sunny, Overcast, LightRain, HeavyRain }

    /// <summary>
    /// Adapter the pigeon system reads weather through, so it never hard-depends
    /// on a particular weather manager. The default implementation is backed by the
    /// existing <see cref="WeatherState"/> global; a game with a richer weather
    /// system can swap in its own via <see cref="PigeonWeatherService.Provider"/>.
    /// </summary>
    public interface IPigeonWeatherProvider
    {
        PigeonWeatherKind Weather { get; }
        /// <summary>Ground-flock spawn multiplier (0 = none).</summary>
        float GroundSpawnMultiplier { get; }
        bool AllowGroundFlocks { get; }
        bool AllowPetalEvents { get; }
        bool PreferShelteredLanding { get; }
        bool SuppressCoo { get; }
    }

    /// <summary>Default clear/rain provider mapped onto <see cref="WeatherState"/>.</summary>
    public sealed class DefaultPigeonWeatherProvider : IPigeonWeatherProvider
    {
        public PigeonWeatherKind Weather => WeatherState.Current switch
        {
            JoburgRunner.Environment.Weather.Overcast => PigeonWeatherKind.Overcast,
            JoburgRunner.Environment.Weather.Rain => PigeonWeatherKind.LightRain,
            JoburgRunner.Environment.Weather.HeavyRain => PigeonWeatherKind.HeavyRain,
            _ => PigeonWeatherKind.Sunny,
        };

        public float GroundSpawnMultiplier => WeatherState.GroundFlockMultiplier;
        public bool AllowGroundFlocks => WeatherState.AllowsGroundFlocks;
        public bool AllowPetalEvents => Weather == PigeonWeatherKind.Sunny || Weather == PigeonWeatherKind.Overcast;
        public bool PreferShelteredLanding => Weather == PigeonWeatherKind.LightRain || Weather == PigeonWeatherKind.HeavyRain;
        public bool SuppressCoo => Weather == PigeonWeatherKind.HeavyRain;
    }

    /// <summary>Global access point for the current weather provider (default: clear-weather adapter).</summary>
    public static class PigeonWeatherService
    {
        static IPigeonWeatherProvider provider = new DefaultPigeonWeatherProvider();

        public static IPigeonWeatherProvider Provider
        {
            get => provider;
            set => provider = value ?? new DefaultPigeonWeatherProvider();
        }
    }
}
