namespace Contoso.NotificationRelay.Service
{
    /// <summary>
    /// Strongly-typed configuration options bound from the "NotificationRelay"
    /// section of appsettings.json. Replaces the legacy AppSettings static class.
    /// </summary>
    public class NotificationRelayOptions
    {
        public int PollIntervalMs { get; set; } = 1000;
        public int RetryCount { get; set; } = 3;
        public int RetryBackoffMs { get; set; } = 200;
        public string LogDirectory { get; set; } = "logs";
    }
}
