using ECommon.Components;
using ECommon.Configurations;
using ECommon.Socketing;
using ECommon.Utilities;
using EQueue.Clients.Consumers;
using EQueue.Configurations;
using EQueue.Protocols;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace QuickStart.ConsumerClientHost
{
    public class Bootstrap
    {

        List<Consumer> _consumers = new List<Consumer>();
        IConfigurationRoot _configuration;
        public void Initialize()
        {
            InitializeEQueue();
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
             .AddEnvironmentVariables();
            _configuration = builder.Build();
            //
            ConsumeTest();
        }

        public void Start()
        {
            foreach (var consumer in _consumers)
            {
                consumer.Start();
            }
        }

        public void Shutdown()
        {
            foreach (var consumer in _consumers)
            {
                consumer.Stop();
            }
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

        public void ConsumeTest()
        {
            var clusterName = _configuration["EQueue:ClusterName"];
            var consumerName = _configuration["EQueue:ConsumerName"];
            var consumerGroup = _configuration["EQueue:ConsumerGroup"];
            var address = _configuration["EQueue:NameServerAddress"];
            var topic = _configuration["EQueue:Topic"];
            var nameServerAddress = string.IsNullOrEmpty(address) ? SocketUtils.GetLocalIPV4() : IPAddress.Parse(address);
            int nameServerPort = int.Parse(_configuration["EQueue:NameServerPort"]);
            var clientCount = int.Parse(_configuration["EQueue:ClientCount"]);
            var setting = new ConsumerSetting
            {
                ClusterName = clusterName,
                ConsumeFromWhere = ConsumeFromWhere.FirstOffset,
                MessageHandleMode = MessageHandleMode.Sequential,
                NameServerList = new List<IPEndPoint> { new IPEndPoint(nameServerAddress, nameServerPort) }
            };
            var messageHandler = new MessageHandler();
            for (var i = 1; i <= clientCount; i++)
            {
                var consumer = new Consumer(consumerGroup, setting, consumerName)
                     .Subscribe(topic)
                     .SetMessageHandler(messageHandler);
                //.Start();
                _consumers.Add(consumer);
            }
        }

        class MessageHandler : IMessageHandler
        {
            private readonly IPerformanceService _performanceService;

            public MessageHandler()
            {
                _performanceService = ObjectContainer.Resolve<IPerformanceService>();
                _performanceService.Initialize("TotalReceived").Start();
            }

            public void Handle(QueueMessage message, IMessageContext context)
            {
                _performanceService.IncrementKeyCount("default", (DateTime.Now - message.CreatedTime).TotalMilliseconds);
                context.OnMessageHandled(message);
            }
        }
    }
}
