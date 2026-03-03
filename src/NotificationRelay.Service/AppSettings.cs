using System;
using System.Configuration;

namespace Contoso.NotificationRelay.Service
{
    /// <summary>
    /// Reads typed settings from App.config appSettings section.
    /// Typical .NET Framework pattern — no DI, no IOptions.
    /// </summary>
    public static class AppSettings
    {
        public static int PollIntervalMs
        {
            get { return GetInt("PollIntervalMs", 1000); }
        }

        public static int RetryCount
        {
            get { return GetInt("RetryCount", 3); }
        }

        public static int RetryBackoffMs
        {
            get { return GetInt("RetryBackoffMs", 200); }
        }

        public static string LogDirectory
        {
            get { return ConfigurationManager.AppSettings["LogDirectory"] ?? "logs"; }
        }

        private static int GetInt(string key, int defaultValue)
        {
            string raw = ConfigurationManager.AppSettings[key];
            int result;
            if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}
