using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JetBlack.TopicBus.Adapters;
using JetBlack.TopicBus.Config;

namespace TopicBusCachingPublisher
{
    class MainClass
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            var config = (ClientConfigurationSectionHandler)ConfigurationManager.GetSection("topicBusClient");

            // Create a client.
            CachingPublisher client = new CachingPublisher(config.DefaultConfig);

            // Attempt to connect.
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

            // Prepare the market data.
            var marketData = new Dictionary<string, Dictionary<string, object>>()
            {
                { "LSE.VOD", new Dictionary<string, object>()
                    {
                        { "NAME", "Vodafone Group PLC" },
                        { "BID", 140.60 },
                        { "ASK", 140.65 }
                    }
                },
                { "LSE.TSCO", new Dictionary<string, object>()
                    {
                        { "NAME", "Tesco PLC" },
                        { "BID", 423.15 },
                        { "ASK", 423.25 }
                    }
                },
                { "LSE.SBRY", new Dictionary<string, object>()
                    {
                        { "NAME", "J Sainsbury PLC" },
                        { "BID", 325.30 },
                        { "ASK", 325.35 }
                    }
                }
            };

            // Request a notification when the the following regex pattern is
            // requested. This corresponds to the things we are publishing.
            client.AddNotification(@"LSE\..*");

            // Publish the data.
            foreach (var item in marketData)
            {
                client.Publish(item.Key, item.Value);
                // Remove the name from our dictionary, so we don't republish it.
                item.Value.Remove("NAME");
            }

            // Use an event to end publishing.
            var stopEvent = new ManualResetEvent(false);

            // Handle unexpected disconnects.
            client.OnClosed += isAbnormal =>
                {
                    if (isAbnormal)
                    {
                        Console.WriteLine("Unexpected disconnect - Press ENTER to quit");
                        stopEvent.Set();
                    }
                };

            // Start publishing changes.
            var task = new Task(() => PublishIntermitantly(client, marketData, stopEvent));
            task.Start();

            Console.WriteLine("Publishing Data. Press <ENTER> to quit.");
            Console.ReadLine();

            // Tell the publisher to stop.
            Console.WriteLine("Stopping the publisher.");
            stopEvent.Set();

            // Wait a second for the publisher to stop.
            Console.WriteLine("Waiting a second ...");
            Thread.Sleep(1000);

            // Tidy up.
            Console.WriteLine("... exiting");
            client.Close();
        }

        static void PublishIntermitantly(CachingPublisher client, IDictionary<string, Dictionary<string, object>> marketData, ManualResetEvent stopEvent)
        {
            Random rnd = new Random();

            while (true)
            {
                foreach (var item in marketData)
                {
                    // Wait a short time.
                    var timeout = 10 * rnd.Next(5, 100);
                    if (stopEvent.WaitOne(timeout))
                        return;

                    // Pevert the data a little.
                    var bid = (double)item.Value["BID"];
                    var ask = (double)item.Value["ASK"];
                    var spread = ask - bid;
                    item.Value["BID"] = Math.Round(bid + bid * rnd.NextDouble() * 5.0 / 100.0, 2);
                    item.Value["ASK"] = Math.Round(bid + spread, 2);
                    Console.WriteLine("{0}, BID={1}, ASK={2}", item.Key, item.Value["BID"], item.Value["ASK"]);
                    client.Publish(item.Key, item.Value);
                }
            }
        }
    }}
