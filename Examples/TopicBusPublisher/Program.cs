using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using JetBlack.TopicBus.Adapters;
using JetBlack.TopicBus.Config;

namespace TopicBusPublisher
{
    class MainClass
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            var config = (ClientConfigurationSectionHandler)ConfigurationManager.GetSection("topicBusClient");

            // Create a client.
            Client client = new Client(config.DefaultConfig);

            // Attempt to connect.
            try
            {
                client.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to distributor: {0}", ex.Message);
                Environment.Exit(-1);
            }

            // Create some data.
            var data = new Dictionary<string, object>()
            {
                { "NAME", "Vodafone Group PLC" },
                { "BID", 140.60 },
                { "ASK", 140.65 }
            };

            // Publish the data.
            client.Publish("LSE.VOD", true, data);

            // Close the client.
            client.Close();
        }
    }
}
