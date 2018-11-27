using System;
using Topshelf;

namespace QuickStart.BrokerServerHost
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.RunAsLocalService();
                x.StartAutomatically();
                x.SetDescription("EQueue Broker Service");
                x.SetDisplayName("EQueueBrokerServer");
                x.SetServiceName("EQueueBrokerServer");
                x.Service<Bootstrap>(s =>
                {
                    s.ConstructUsing(b => new Bootstrap());
                    s.WhenStarted(b =>
                    {
                        b.Initialize();
                        b.Start();
                    });
                    s.WhenStopped(b => b.Shutdown());
                });

            });
        }
    }
}
