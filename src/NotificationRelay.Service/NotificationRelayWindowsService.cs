using System.ServiceProcess;
using Contoso.NotificationRelay.Domain.Interfaces;

namespace Contoso.NotificationRelay.Service
{
    /// <summary>
    /// Windows Service wrapper. Uses ServiceBase to integrate with the
    /// Windows Service Control Manager (SCM).
    /// 
    /// Registration: sc create ContosoNotificationRelay binPath= "C:\path\NotificationRelay.Service.exe --service"
    /// </summary>
    public class NotificationRelayWindowsService : ServiceBase
    {
        private NotificationRelayEngine _engine;
        private readonly ILogger _logger;

        public NotificationRelayWindowsService(ILogger logger)
        {
            _logger = logger;
            ServiceName = "ContosoNotificationRelay";
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info("Windows Service OnStart invoked.");
            _engine = new NotificationRelayEngine(_logger);
            _engine.Start();
        }

        protected override void OnStop()
        {
            _logger.Info("Windows Service OnStop invoked.");
            if (_engine != null)
            {
                _engine.Stop();
                _engine.Dispose();
            }
        }
    }
}
