using ECommon.Components;
using ECommon.Configurations;
using ECommon.Logging;
using ECommon.Remoting;
using ECommon.Socketing;
using ECommon.Utilities;
using EQueue.Clients.Producers;
using EQueue.Configurations;
using EQueue.Protocols;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace QuickStart.ProducerClientHost
{

    public class Bootstrap
    {
        static string _mode;
        static ILogger _logger;
        static bool _hasError;
        static PerformanceServiceSetting _setting;
        static IPerformanceService _performanceService;
        IConfigurationRoot _configuration;
        List<Producer> _producers = new List<Producer>();
        List<Action> _actions = new List<Action>();
        public void Initialize()
        {

            InitializeEQueue();
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
            _configuration = builder.Build();

            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(typeof(Program).Name);
            _performanceService = ObjectContainer.Resolve<IPerformanceService>();
            _mode = _configuration["EQueue:Mode"];


            var logContextText = "mode: " + _mode;
            _setting = new PerformanceServiceSetting
            {
                AutoLogging = false,
                StatIntervalSeconds = 1,
                PerformanceInfoHandler = x =>
                {
                    _logger.InfoFormat("{0}, {1}, totalCount: {2}, throughput: {3}, averageThrughput: {4}, rt: {5:F3}ms, averageRT: {6:F3}ms", _performanceService.Name, logContextText, x.TotalCount, x.Throughput, x.AverageThroughput, x.RT, x.AverageRT);
                }
            };

            SendMessageTest();

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
                .UseDeleteMessageByCountStrategy(5)
                .BuildContainer();
        }
        public void Start()
        {

            _performanceService.Initialize("SendMessage", _setting).Start();
            foreach (var producer in _producers)
            {
                producer.Start();
            }

            Task.Factory.StartNew(() => Parallel.Invoke(_actions.ToArray()));
        }

        public void Shutdown()
        {
            foreach (var producer in _producers)
            {
                producer.Shutdown();
            }
        }

        void SendMessageTest()
        {
            var clusterName = _configuration["EQueue:ClusterName"];
            var address = _configuration["EQueue:NameServerAddress"];
            var nameServerAddress = string.IsNullOrEmpty(address) ? SocketUtils.GetLocalIPV4() : IPAddress.Parse(address);
            int nameServerPort = int.Parse(_configuration["EQueue:NameServerPort"]);
            var clientCount = int.Parse(_configuration["EQueue:ClientCount"]);
            var messageSize = int.Parse(_configuration["EQueue:MessageSize"]);
            var messageCount = long.Parse(_configuration["EQueue:MessageCount"]);
            var batchSize = int.Parse(_configuration["EQueue:BatchSize"]);
           
            var payload = new byte[messageSize];
            var topic = _configuration["EQueue:Topic"];

            for (var i = 0; i < clientCount; i++)
            {
                var setting = new ProducerSetting
                {
                    ClusterName = clusterName,
                    NameServerList = new List<IPEndPoint> { new IPEndPoint(nameServerAddress, nameServerPort) }
                };
                var producer = new Producer(setting);
                if (_mode == "Callback")
                {
                    producer.RegisterResponseHandler(new ResponseHandler { BatchSize = batchSize });
                }
                //producer.Start();
                _producers.Add(producer);
                _actions.Add(() => SendMessages(producer, _mode, batchSize, messageCount, topic, payload));
            }

            //  Task.Factory.StartNew(() => Parallel.Invoke(actions.ToArray()));
        }
        void SendMessages(Producer producer, string mode, int batchSize, long messageCount, string topic, byte[] payload)
        {
            _logger.Info("----Send message starting----");

            var sendAction = default(Action<long>);

            if (_mode == "Oneway")
            {
                sendAction = index =>
                {
                    if (batchSize == 1)
                    {
                        var message = new Message(topic, 100, payload);
                        producer.SendOneway(message, index.ToString()).ContinueWith(t =>
                        {
                            _performanceService.IncrementKeyCount(_mode, (DateTime.Now - message.CreatedTime).TotalMilliseconds);
                        });
                    }
                    else
                    {
                        var messages = new List<Message>();
                        for (var i = 0; i < batchSize; i++)
                        {
                            messages.Add(new Message(topic, 100, payload));
                        }
                        producer.BatchSendOneway(messages, index.ToString()).ContinueWith(t =>
                        {
                            var currentTime = DateTime.Now;
                            foreach (var message in messages)
                            {
                                _performanceService.IncrementKeyCount(_mode, (currentTime - message.CreatedTime).TotalMilliseconds);
                            }
                        });
                    }
                };
            }
            else if (_mode == "Async")
            {
                sendAction = index =>
                {
                    if (batchSize == 1)
                    {
                        var message = new Message(topic, 100, payload);
                        producer.SendAsync(message, index.ToString()).ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                _hasError = true;
                                _logger.ErrorFormat("Send message has exception, errorMessage: {0}", t.Exception.GetBaseException().Message);
                                return;
                            }
                            if (t.Result == null)
                            {
                                _hasError = true;
                                _logger.Error("Send message timeout.");
                                return;
                            }
                            if (t.Result.SendStatus != SendStatus.Success)
                            {
                                _hasError = true;
                                _logger.ErrorFormat("Send message failed, errorMessage: {0}", t.Result.ErrorMessage);
                                return;
                            }
                            _performanceService.IncrementKeyCount(_mode, (DateTime.Now - message.CreatedTime).TotalMilliseconds);
                        });
                    }
                    else
                    {
                        var messages = new List<Message>();
                        for (var i = 0; i < batchSize; i++)
                        {
                            messages.Add(new Message(topic, 100, payload));
                        }
                        producer.BatchSendAsync(messages, index.ToString()).ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                _hasError = true;
                                _logger.ErrorFormat("Send message has exception, errorMessage: {0}", t.Exception.GetBaseException().Message);
                                return;
                            }
                            if (t.Result == null)
                            {
                                _hasError = true;
                                _logger.Error("Send message timeout.");
                                return;
                            }
                            if (t.Result.SendStatus != SendStatus.Success)
                            {
                                _hasError = true;
                                _logger.ErrorFormat("Send message failed, errorMessage: {0}", t.Result.ErrorMessage);
                                return;
                            }
                            var currentTime = DateTime.Now;
                            foreach (var message in messages)
                            {
                                _performanceService.IncrementKeyCount(_mode, (currentTime - message.CreatedTime).TotalMilliseconds);
                            }
                        });
                    }
                };
            }
            else if (_mode == "Callback")
            {
                sendAction = index =>
                {
                    if (batchSize == 1)
                    {
                        var message = new Message(topic, 100, payload);
                        producer.SendWithCallback(message, index.ToString());
                    }
                    else
                    {
                        var messages = new List<Message>();
                        for (var i = 0; i < batchSize; i++)
                        {
                            messages.Add(new Message(topic, 100, payload));
                        }
                        producer.BatchSendWithCallback(messages, index.ToString());
                    }
                };
            }

            Task.Factory.StartNew(() =>
            {
                for (var i = 0L; i < messageCount; i++)
                {
                    try
                    {
                        sendAction(i);
                    }
                    catch (Exception ex)
                    {
                        _hasError = true;
                        _logger.ErrorFormat("Send message failed, errorMsg:{0}", ex.Message);
                    }

                    if (_hasError)
                    {
                        Thread.Sleep(3000);
                        _hasError = false;
                    }
                }
            });
        }

        class ResponseHandler : IResponseHandler
        {
            public int BatchSize;

            public void HandleResponse(RemotingResponse remotingResponse)
            {
                if (BatchSize == 1)
                {
                    var sendResult = Producer.ParseSendResult(remotingResponse);
                    if (sendResult.SendStatus != SendStatus.Success)
                    {
                        _hasError = true;
                        _logger.Error(sendResult.ErrorMessage);
                        return;
                    }
                    _performanceService.IncrementKeyCount(_mode, (DateTime.Now - sendResult.MessageStoreResult.CreatedTime).TotalMilliseconds);
                }
                else
                {
                    var sendResult = Producer.ParseBatchSendResult(remotingResponse);
                    if (sendResult.SendStatus != SendStatus.Success)
                    {
                        _hasError = true;
                        _logger.Error(sendResult.ErrorMessage);
                        return;
                    }
                    var currentTime = DateTime.Now;
                    foreach (var result in sendResult.MessageStoreResult.MessageResults)
                    {
                        _performanceService.IncrementKeyCount(_mode, (currentTime - result.CreatedTime).TotalMilliseconds);
                    }
                }
            }
        }

    }
}
