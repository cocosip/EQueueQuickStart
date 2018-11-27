using System;
using Topshelf;

namespace QuickStart.ProducerClientHost
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.RunAsLocalService();
                x.StartAutomatically();
                x.SetDescription("EQueue Producer Client Service");
                x.SetDisplayName("EQueueProducerClient");
                x.SetServiceName("EQueueProducerClient");
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
