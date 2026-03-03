using System;
using Contoso.NotificationRelay.Domain.Interfaces;

namespace Contoso.NotificationRelay.Infrastructure.Logging
{
    /// <summary>
    /// Lightweight logger for unit tests — writes to console only, no file I/O.
    /// </summary>
    public class ConsoleOnlyLogger : ILogger
    {
        public void Info(string message)
        {
            Write("INFO", message);
        }

        public void Warn(string message)
        {
            Write("WARN", message);
        }

        public void Error(string message)
        {
            Write("ERROR", message);
        }

        public void Debug(string message)
        {
            Write("DEBUG", message);
        }

        private void Write(string level, string message)
        {
            Console.WriteLine(string.Format("[{0}] {1}", level, message));
        }
    }
}
