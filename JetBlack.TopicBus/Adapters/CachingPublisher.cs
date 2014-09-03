using System;
using System.Collections.Generic;
using System.Linq;
using JetBlack.TopicBus.Config;

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

        readonly Dictionary<string, CacheItem> _topicCache = new Dictionary<string, CacheItem>();

        readonly Client _client;

        public CachingPublisher(ClientConfig clientConfig)
        {
            _client = new Client(clientConfig);

            _client.OnForwardedSubscription += OnForwardedSubscription;
            _client.OnData += RaiseOnData;
            _client.OnClosed += RaiseOnClosed;
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
                lock (_topicCache)
                {
                    // Have we received a subscription or published data on this topic yet?
                    if (!_topicCache.ContainsKey(topic))
                        _topicCache.Add(topic, new CacheItem());

                    // Has this client already subscribed to this topic?
                    if (!_topicCache[topic].ClientStates.ContainsKey(clientId))
                    {
                        // Add the client to the cache item, and indicate that we have not yet sent an image.
                        _topicCache[topic].ClientStates.Add(clientId, false);
                    }

                    if (!_topicCache[topic].ClientStates[clientId])
                    {
                        // Send the image and mark this client appropriately.
                        _topicCache[topic].ClientStates[clientId] = true;
                        _client.Send(clientId, topic, true, _topicCache[topic].Data);
                    }
                }
            }
            else
            {
                lock (_topicCache)
                {
                    // Have we received a subscription or published data on this topic yet?
                    if (_topicCache.ContainsKey(topic))
                    {
                        // Does this topic have this client?
                        if (_topicCache[topic].ClientStates.ContainsKey(clientId))
                        {
                            _topicCache[topic].ClientStates.Remove(clientId);

                            // If there are no clients and no data remove the item.
                            if (_topicCache[topic].ClientStates.Count == 0 && _topicCache[topic].Data.Count == 0)
                                _topicCache.Remove(topic);
                        }
                    }
                }
            }
        }

        public bool Connect()
        {
            return _client.Connect();
        }

        public bool Connect(string host, int port)
        {
            return _client.Connect(host, port);
        }

        public void Close()
        {
            _client.Close();
        }

        public void Publish(string topic, IDictionary<string, object> data)
        {
            Publish(topic, false, data);
        }

        void Publish(string topic, bool isImage, IDictionary<string, object> data)
        {
            // If the topic is not in the cache add it.
            CacheItem cacheItem;
            if (!_topicCache.TryGetValue(topic, out cacheItem))
                _topicCache.Add(topic, cacheItem = new CacheItem());

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
                        _client.Send(clientId, topic, isImage, data);
                    else
                    {
                        _client.Send(clientId, topic, true, cacheItem.Data);
                        cacheItem.ClientStates[clientId] = true;
                    }
                }
            }
            else
                _client.Publish(topic, isImage, data);
        }

        public void AddSubscription(string topic)
        {
            _client.AddSubscription(topic);
        }

        public void RemoveSubscription(string topic)
        {
            _client.RemoveSubscription(topic);
        }

        public void AddNotification(string topicPattern)
        {
            _client.AddNotification(topicPattern);
        }

        public void RemoveNotification(string topicPattern)
        {
            _client.RemoveNotification(topicPattern);
        }

        void RaiseOnData(string topic, IDictionary<string, object> data, bool isImage)
        {
            if (OnData != null)
                OnData(topic, data, isImage);
        }
    }
}

