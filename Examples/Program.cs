using System;
using System.Configuration;
using JetBlack.TopicBus.Distributor;
using JetBlack.TopicBus.Config;
using Spring.Context.Support;

namespace TopicBusConsole
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            var ctx = ContextRegistry.GetContext();
            var config = (DistributorConfig)ctx["DistributorConfig"];

            var server = new Server(config);

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            server.Dispose();
        }
    }
}
