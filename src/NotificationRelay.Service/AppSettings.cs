namespace Contoso.NotificationRelay.Service;

public class NotificationRelayOptions
{
    public const string SectionName = "NotificationRelay";
    public int PollIntervalMs { get; set; } = 1000;
    public int RetryCount { get; set; } = 3;
    public int RetryBackoffMs { get; set; } = 200;
    public string LogDirectory { get; set; } = "logs";
}
