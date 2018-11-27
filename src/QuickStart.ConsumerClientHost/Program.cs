using System;
using Topshelf;

namespace QuickStart.ConsumerClientHost
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.RunAsLocalService();
                x.StartAutomatically();
                x.SetDescription("EQueue Consumer Client Service");
                x.SetDisplayName("EQueueConsumerClient");
                x.SetServiceName("EQueueConsumerClient");
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
