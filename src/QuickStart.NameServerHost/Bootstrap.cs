using ECommon.Configurations;
using EQueue.Configurations;
using EQueue.NameServer;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net;

namespace QuickStart.NameServerHost
{
    public class Bootstrap
    {
        private NameServerController _nameServerController;

        public void Initialize()
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
             .AddEnvironmentVariables();
            var configuration = builder.Build();

            InitializeEQueue();

            var localAddress = IPAddress.Parse(configuration["EQueue:LocalAddress"]);
            var localPort = int.Parse(configuration["EQueue:LocalPort"]);
            var autoCreateTopic = bool.Parse(configuration["EQueue:AutoCreateTopic"]);
            var brokerInactiveMaxMilliseconds = int.Parse(configuration["EQueue:BrokerInactiveMaxMilliseconds"]);
            var setting = new NameServerSetting()
            {
                BindingAddress = new IPEndPoint(localAddress, localPort),
                AutoCreateTopic = autoCreateTopic,
                BrokerInactiveMaxMilliseconds = brokerInactiveMaxMilliseconds,
            };
            _nameServerController = new NameServerController(setting);
        }

        public void Start()
        {
            _nameServerController.Start();
        }

        public void Shutdown()
        {
            _nameServerController.Shutdown();
        }

        void InitializeEQueue()
        {
            var configuration = ECommon.Configurations.Configuration
                .Create()
                .UseAutofac()
                .RegisterCommonComponents()
                .UseLog4Net()
                .UseJsonNet()
                .RegisterUnhandledExceptionHandler()
                .RegisterEQueueComponents()
                .BuildContainer();
        }
    }
}
