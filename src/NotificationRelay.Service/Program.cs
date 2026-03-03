using System;
using System.Linq;
using System.ServiceProcess;
using Contoso.NotificationRelay.Infrastructure.Logging;

namespace Contoso.NotificationRelay.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool runAsService = args.Any(a =>
                a.Equals("--service", StringComparison.OrdinalIgnoreCase));

            if (runAsService)
            {
                // Windows Service mode — hand off to SCM
                var logger = new FileAndConsoleLogger(AppSettings.LogDirectory);
                var service = new NotificationRelayWindowsService(logger);
                ServiceBase.Run(service);
            }
            else
            {
                // Interactive console mode
                RunConsole();
            }
        }

        private static void RunConsole()
        {
            using (var logger = new FileAndConsoleLogger(AppSettings.LogDirectory))
            {
                Console.WriteLine("=== Contoso Notification Relay Service (Console Mode) ===");
                Console.WriteLine("Press Ctrl+C or 'Q' to stop.");
                Console.WriteLine();

                using (var engine = new NotificationRelayEngine(logger))
                {
                    engine.Start();

                    Console.CancelKeyPress += (sender, e) =>
                    {
                        e.Cancel = true;
                        engine.Stop();
                    };

                    // Block until user presses Q or Ctrl+C stops the engine
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(intercept: true);
                            if (key.Key == ConsoleKey.Q)
                            {
                                break;
                            }
                        }
                        System.Threading.Thread.Sleep(250);
                    }
                }
            }

            Console.WriteLine("Service stopped. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
