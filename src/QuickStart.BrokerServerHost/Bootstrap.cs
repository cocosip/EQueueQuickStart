using ECommon.Configurations;
using ECommon.Extensions;
using EQueue.Broker;
using EQueue.Clients.Producers;
using EQueue.Configurations;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace QuickStart.BrokerServerHost
{
    public class Bootstrap
    {
        private BrokerController _brokerController;
        public void Initialize()
        {

            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddEnvironmentVariables();
            var configuration = builder.Build();
            InitializeEQueue();
            //本地IP地址
            var localAddress = IPAddress.Parse(configuration["EQueue:LocalAddress"]);
            var setting = new BrokerSetting(
                isMessageStoreMemoryMode: bool.Parse(configuration["EQueue:IsMemoryMode"]),
                chunkFileStoreRootPath: configuration["EQueue:FileStoreRootPath"],
                chunkFlushInterval: int.Parse(configuration["EQueue:FlushInterval"]),
                chunkCacheMaxCount: int.Parse(configuration["EQueue:ChunkCacheMaxCount"]),
                chunkCacheMinCount: int.Parse(configuration["EQueue:ChunkCacheMinCount"]),
                messageChunkDataSize: int.Parse(configuration["EQueue:ChunkSize"]) * 1024 * 1024,
                chunkWriteBuffer: int.Parse(configuration["EQueue:ChunkWriteBuffer"]) * 1024,
                enableCache: bool.Parse(configuration["EQueue:EnableCache"]),
                syncFlush: bool.Parse(configuration["EQueue:SyncFlush"]),
                messageChunkLocalCacheSize: 30 * 10000,
                queueChunkLocalCacheSize: 10000)
            {
                NotifyWhenMessageArrived = bool.Parse(configuration["EQueue:NotifyWhenMessageArrived"]),
                MessageWriteQueueThreshold = int.Parse(configuration["EQueue:MessageWriteQueueThreshold"]),
                DeleteMessageIgnoreUnConsumed = bool.Parse(configuration["EQueue:DeleteMessageIgnoreUnConsumed"])
            };
            //NameServer地址
            setting.NameServerList = ParseNameServerAddress(configuration["EQueue:NameServerAddress"]);
            //Broker名称
            setting.BrokerInfo.BrokerName = configuration["EQueue:BrokerName"];
            setting.BrokerInfo.GroupName = configuration["EQueue:GroupName"];
            //生产者地址和端口
            setting.BrokerInfo.ProducerAddress = new IPEndPoint(localAddress, int.Parse(configuration["EQueue:ProducerPort"])).ToAddress();
            //消费者地址和端口
            setting.BrokerInfo.ConsumerAddress = new IPEndPoint(localAddress, int.Parse(configuration["EQueue:ConsumerPort"])).ToAddress();
            //管理端口
            setting.BrokerInfo.AdminAddress = new IPEndPoint(localAddress, int.Parse(configuration["EQueue:AdminPort"])).ToAddress();

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
            var configuration = ECommon.Configurations.Configuration
                .Create()
                .UseAutofac()
                .RegisterCommonComponents()
                .UseLog4Net()
                .UseJsonNet()
                .RegisterUnhandledExceptionHandler()
                .RegisterEQueueComponents()
                .SetDefault<IQueueSelector, QueueAverageSelector>()
                .UseDeleteMessageByCountStrategy(5)
                .BuildContainer();
        }
    }
}
