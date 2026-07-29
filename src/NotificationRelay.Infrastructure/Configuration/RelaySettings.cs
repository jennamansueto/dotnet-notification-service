namespace Contoso.NotificationRelay.Infrastructure.Configuration
{
    /// <summary>
    /// Strongly-typed settings bound from the "Relay" configuration section.
    /// Replaces the legacy App.config appSettings + static AppSettings reader.
    /// </summary>
    public class RelaySettings
    {
        public int PollIntervalMs { get; set; } = 1000;
        public int RetryCount { get; set; } = 3;
        public int RetryBackoffMs { get; set; } = 200;
    }
}
