using System;
using System.IO;
using Contoso.NotificationRelay.Domain.Interfaces;

namespace Contoso.NotificationRelay.Infrastructure.Logging
{
    public class FileAndConsoleLogger : ILogger, IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly object _lock = new object();
        private bool _disposed;

        public FileAndConsoleLogger(string logDirectory)
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string fileName = string.Format("relay-{0:yyyyMMdd-HHmmss}.log", DateTime.Now);
            string filePath = Path.Combine(logDirectory, fileName);
            _writer = new StreamWriter(filePath, append: true) { AutoFlush = true };
        }

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
            string line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}", DateTime.UtcNow, level, message);
            lock (_lock)
            {
                Console.WriteLine(line);
                if (!_disposed)
                {
                    _writer.WriteLine(line);
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _writer.Flush();
                    _writer.Dispose();
                }
            }
        }
    }
}
