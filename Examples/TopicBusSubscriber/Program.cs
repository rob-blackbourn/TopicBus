using System;
using System.Collections.Generic;
using JetBlack.TopicBus.Adapters;
using System.Net;
using JetBlack.TopicBus.IO;
using System.Configuration;
using JetBlack.TopicBus.Config;

namespace TopicBusSubscriber
{
    class MainClass
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            var config = (ClientConfigurationSectionHandler)ConfigurationManager.GetSection("topicBusClient");

            // Create a client.
            Client client = new Client(config.DefaultConfig);

            try
            {
                // Assume the Distributor is running on the local machine.
                client.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to distributor: {0}", ex.Message);
                Environment.Exit(-1);
            }

            // Add a delegate to be called should the client be closed.
            client.OnClosed += OnClosed;
            // Add a delegate to be called when data arrives.
            client.OnData += OnData;

            // Subscribe to some topics.
            client.AddSubscription("LSE.VOD");
            client.AddSubscription("LSE.TSCO");
            client.AddSubscription("LSE.FOO");
            client.AddSubscription("VTX.BAY");

            // Wait to exit.
            Console.WriteLine("Press <enter> to quit");
            Console.ReadLine();

            // Tidy up.
            client.Close();
        }

        static void OnClosed(bool isAbnormal)
        {
            Console.WriteLine("This client as closed {0}", isAbnormal ? "abnormally" : "normally");
            if (isAbnormal)
                Environment.Exit(-1);
        }

        static void OnData(string topic, object data, bool isImage)
        {
            // Write out the data received. Note the message may be empty.
            Console.WriteLine("OnData: {0} {1}", topic, isImage);
            if (data == null)
                Console.WriteLine("No Data");
            else if (data is IDictionary<string, object>)
            {
                foreach (var pair in (IDictionary<string, object>)data)
                    Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }
        }
    }}
