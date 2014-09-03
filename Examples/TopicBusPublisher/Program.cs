using System;
using System.Collections.Generic;
using System.Net;
using JetBlack.TopicBus.Adapters;
using JetBlack.TopicBus.IO;

namespace TopicBusPublisher
{
    class MainClass
    {
        static void Main(string[] args)
        {
            // Create a client.
            Client client = new Client(new BinarySerializer());

            // Attempt to connect.
            try
            {
                client.Connect(Dns.GetHostName(), 9121);
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
