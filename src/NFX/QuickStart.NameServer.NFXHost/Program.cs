using Topshelf;

namespace QuickStart.NameServer.NFXHost
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.RunAsLocalService();
                x.StartAutomatically();
                x.SetDescription("EQueue NameServer Service");
                x.SetDisplayName("EQueueNameServer");
                x.SetServiceName("EQueueNameServer");
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
