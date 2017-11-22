using ECommon.Configurations;
using EQueue.Configurations;
using EQueue.NameServer;
using System.Configuration;
using System.Net;
using ECommonConfiguration = ECommon.Configurations.Configuration;
namespace QuickStart.NameServer.DNXHost
{
    public class Bootstrap
    {
        private NameServerController _nameServerController;

        public void Initialize()
        {
            InitializeEQueue();
            var localAddress = IPAddress.Parse(ConfigurationManager.AppSettings["LocalAddress"]);
            var localPort = int.Parse(ConfigurationManager.AppSettings["LocalPort"]);
            var autoCreateTopic = bool.Parse(ConfigurationManager.AppSettings["autoCreateTopic"]);
            var brokerInactiveMaxMilliseconds = int.Parse(ConfigurationManager.AppSettings["BrokerInactiveMaxMilliseconds"]);
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
            var configuration = ECommonConfiguration
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
