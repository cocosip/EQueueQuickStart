using ECommon.Configurations;
using ECommon.Extensions;
using EQueue.Broker;
using EQueue.Configurations;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using ECommonConfiguration = ECommon.Configurations.Configuration;
namespace QuickStart.BrokerServer.DNXHost
{
    public class Bootstrap
    {
        private BrokerController _brokerController;
        public void Initialize()
        {
            InitializeEQueue();
            //本地IP地址
            var localAddress = IPAddress.Parse(ConfigurationManager.AppSettings["LocalAddress"]);
            var setting = new BrokerSetting(
                isMessageStoreMemoryMode: bool.Parse(ConfigurationManager.AppSettings["IsMemoryMode"]),
                chunkFileStoreRootPath: ConfigurationManager.AppSettings["FileStoreRootPath"],
                chunkFlushInterval: int.Parse(ConfigurationManager.AppSettings["FlushInterval"]),
                chunkCacheMaxCount: int.Parse(ConfigurationManager.AppSettings["ChunkCacheMaxCount"]),
                chunkCacheMinCount: int.Parse(ConfigurationManager.AppSettings["ChunkCacheMinCount"]),
                messageChunkDataSize: int.Parse(ConfigurationManager.AppSettings["ChunkSize"]) * 1024 * 1024,
                chunkWriteBuffer: int.Parse(ConfigurationManager.AppSettings["ChunkWriteBuffer"]) * 1024,
                enableCache: bool.Parse(ConfigurationManager.AppSettings["EnableCache"]),
                syncFlush: bool.Parse(ConfigurationManager.AppSettings["SyncFlush"]),
                messageChunkLocalCacheSize: 30 * 10000,
                queueChunkLocalCacheSize: 10000)
            {
                NotifyWhenMessageArrived = bool.Parse(ConfigurationManager.AppSettings["NotifyWhenMessageArrived"]),
                MessageWriteQueueThreshold = int.Parse(ConfigurationManager.AppSettings["MessageWriteQueueThreshold"]),
                DeleteMessageIgnoreUnConsumed = bool.Parse(ConfigurationManager.AppSettings["DeleteMessageIgnoreUnConsumed"])
            };
            //NameServer地址
            setting.NameServerList = ParseNameServerAddress(ConfigurationManager.AppSettings["NameServerAddress"]);
            //Broker名称
            setting.BrokerInfo.BrokerName = ConfigurationManager.AppSettings["BrokerName"];
            setting.BrokerInfo.GroupName = ConfigurationManager.AppSettings["GroupName"];
            //生产者地址和端口
            setting.BrokerInfo.ProducerAddress = new IPEndPoint(localAddress, int.Parse(ConfigurationManager.AppSettings["ProducerPort"])).ToAddress();
            //消费者地址和端口
            setting.BrokerInfo.ConsumerAddress = new IPEndPoint(localAddress, int.Parse(ConfigurationManager.AppSettings["ConsumerPort"])).ToAddress();
            //管理端口
            setting.BrokerInfo.AdminAddress = new IPEndPoint(localAddress, int.Parse(ConfigurationManager.AppSettings["AdminPort"])).ToAddress();

            _brokerController = BrokerController.Create(setting);
        }

        /// <summary>格式化NameServer地址
        /// </summary>
        private List<IPEndPoint> ParseNameServerAddress(string addressValue)
        {
            var ipEndPoints = new List<IPEndPoint>();
            var addressItems = addressValue.Split(',');
            foreach (var item in addressItems)
            {
                var valueArray = item.Split(':');
                var ipEndPoint = new IPEndPoint(IPAddress.Parse(valueArray[0]), int.Parse(valueArray[1]));
                ipEndPoints.Add(ipEndPoint);
            }
            return ipEndPoints;
        }


        public void Start()
        {
            _brokerController.Start();
        }

        public void Shutdown()
        {
            _brokerController.Shutdown();
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
                .UseDeleteMessageByCountStrategy(5)
                .BuildContainer();
        }
    }
}
