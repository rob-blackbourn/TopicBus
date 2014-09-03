using System;
using System.Configuration;
using JetBlack.TopicBus.Distributor;
using JetBlack.TopicBus.Config;

namespace TopicBusConsole
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            var config = (TopicBusConfigurationSectionHandler)ConfigurationManager.GetSection("simpleTopicBusDistributor");

            var server = new Server(config.DefaultConfig);

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            server.Dispose();
        }
    }
}
