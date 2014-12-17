using System;
using System.Collections.Generic;
using JetBlack.TopicBus.Adapters;
using JetBlack.TopicBus.Config;
using Spring.Context.Support;

namespace TopicBusPublisher
{
    class MainClass
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            var ctx = ContextRegistry.GetContext();
            var clientConfig = (ClientConfig)ctx["ClientConfig"];

            // Create a client.
            Client client = new Client(clientConfig);

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
