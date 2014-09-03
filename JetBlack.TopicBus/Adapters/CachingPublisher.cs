using System;
using System.Collections.Generic;
using System.Linq;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Adapters
{
    public class CachingPublisher
    {
        public event DataCallback OnData;
        public event ClosedCallback OnClosed;

        class CacheItem
        {
            // Remember whether this client id has already received the image.
            public readonly Dictionary<int, bool> ClientStates = new Dictionary<int, bool>();
            // The cache of data constituting the image.
            public readonly Dictionary<string, object> Data = new Dictionary<string, object>();
        }

        readonly Dictionary<string, CacheItem> topicCache = new Dictionary<string, CacheItem>();

        readonly Client client;

        public CachingPublisher(ISerializer serializer)
        {
            client = new Client(serializer);

            client.OnForwardedSubscription += OnForwardedSubscription;
            client.OnData += RaiseOnData;
            client.OnClosed += RaiseOnClosed;
        }

        void RaiseOnClosed(bool isAbnormal)
        {
            if (OnClosed != null)
                OnClosed(isAbnormal);
        }

        void OnForwardedSubscription(int clientId, string topic, bool isAdd)
        {
            if (isAdd)
            {
                lock (topicCache)
                {
                    // Have we received a subscription or published data on this topic yet?
                    if (!topicCache.ContainsKey(topic))
                        topicCache.Add(topic, new CacheItem());

                    // Has this client already subscribed to this topic?
                    if (!topicCache[topic].ClientStates.ContainsKey(clientId))
                    {
                        // Add the client to the cache item, and indicate that we have not yet sent an image.
                        topicCache[topic].ClientStates.Add(clientId, false);
                    }

                    if (!topicCache[topic].ClientStates[clientId])
                    {
                        // Send the image and mark this client appropriately.
                        topicCache[topic].ClientStates[clientId] = true;
                        client.Send(clientId, topic, true, topicCache[topic].Data);
                    }
                }
            }
            else
            {
                lock (topicCache)
                {
                    // Have we received a subscription or published data on this topic yet?
                    if (topicCache.ContainsKey(topic))
                    {
                        // Does this topic have this client?
                        if (topicCache[topic].ClientStates.ContainsKey(clientId))
                        {
                            topicCache[topic].ClientStates.Remove(clientId);

                            // If there are no clients and no data remove the item.
                            if (topicCache[topic].ClientStates.Count == 0 && topicCache[topic].Data.Count == 0)
                                topicCache.Remove(topic);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Connect to a distributor.
        /// </summary>
        /// <param name="host">The host name of the distributor.</param>
        /// <param name="port">The port on which the distributor is listening.</param>
        /// <returns></returns>
        public bool Connect(string host, int port)
        {
            return client.Connect(host, port);
        }

        /// <summary>
        /// Close this client.
        /// </summary>
        public void Close()
        {
            client.Close();
        }

        /// <summary>
        /// Publish data.
        /// </summary>
        /// <param name="topic">The topic on which the data will be published.</param>
        /// <param name="data">The data to be published.</param>
        public void Publish(string topic, IDictionary<string, object> data)
        {
            Publish(topic, false, data);
        }

        void Publish(string topic, bool isImage, IDictionary<string, object> data)
        {
            // If the topic is not in the cache add it.
            CacheItem cacheItem;
            if (!topicCache.TryGetValue(topic, out cacheItem))
                topicCache.Add(topic, cacheItem = new CacheItem());

            // Bring the cache data up to date.
            if (data != null)
                foreach (KeyValuePair<string, object> item in data)
                    cacheItem.Data[item.Key] = item.Value;

            // Are there any clients yet to receive any message on this feed/topic?
            if (cacheItem.ClientStates.Values.Any(c => !c))
            {
                // Yes. Deliver idividual messages.
                foreach (var clientId in cacheItem.ClientStates.Keys.ToArray())
                {
                    if (cacheItem.ClientStates[clientId])
                        client.Send(clientId, topic, isImage, data);
                    else
                    {
                        client.Send(clientId, topic, true, cacheItem.Data);
                        cacheItem.ClientStates[clientId] = true;
                    }
                }
            }
            else
                client.Publish(topic, isImage, data);
        }

        /// <summary>
        /// Add a subscription to a topic.
        /// </summary>
        /// <param name="topic">The topic of interest.</param>
        public void AddSubscription(string topic)
        {
            client.AddSubscription(topic);
        }

        /// <summary>
        /// Remove a subscription to a topic.
        /// </summary>
        /// <param name="topic">The unwanted topic.</param>
        public void RemoveSubscription(string topic)
        {
            client.RemoveSubscription(topic);
        }

        /// <summary>
        /// Add a notification to a topic pattern.
        /// </summary>
        /// <param name="topicPattern">The regex to match against topics.</param>
        public void AddNotification(string topicPattern)
        {
            client.AddNotification(topicPattern);
        }

        /// <summary>
        /// Remove a notification to a topic pattern.
        /// </summary>
        /// <param name="topicPattern">The previous requested regex.</param>
        public void RemoveNotification(string topicPattern)
        {
            client.RemoveNotification(topicPattern);
        }

        void RaiseOnData(string topic, IDictionary<string, object> data, bool isImage)
        {
            if (OnData != null)
                OnData(topic, data, isImage);
        }
    }}

