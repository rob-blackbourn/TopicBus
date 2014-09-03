using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using log4net;
using JetBlack.TopicBus.Messages;

namespace JetBlack.TopicBus.Distributor
{
    public class SubscriptionManager : IDisposable
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(SubscriptionManager));

        readonly IDictionary<string, ISet<Interactor>> _subscriptionCache = new Dictionary<string, ISet<Interactor>>();
        readonly MessageBus _messageBus;

        public SubscriptionManager(MessageBus messageBus)
        {
            _messageBus = messageBus;

            var scheduler = new EventLoopScheduler();

            _messageBus.SubscriptionRequests.SubscribeOn(scheduler).Subscribe(HandleSubscriptionRequest);
            _messageBus.NewNotificationRequests.SubscribeOn(scheduler).Subscribe(ForwardSubscriptionRequests);
            _messageBus.PublishedMulticastDataMessages.SubscribeOn(scheduler).Subscribe(HandleMulticastDataMessage);
            _messageBus.PublishedUnicastDataMessages.SubscribeOn(scheduler).Subscribe(HandleUnicastDataMessage);
            _messageBus.StalePublishers.SubscribeOn(scheduler).Subscribe(HandleStalePublisher);
            _messageBus.ClosedInteractors.SubscribeOn(scheduler).Subscribe(RemoveInteractor);
            _messageBus.FaultedInteractors.SubscribeOn(scheduler).Subscribe(FaultedInteractor);
        }

        void FaultedInteractor(SourceMessage<Exception> sourceMessage)
        {
            Log.Warn("Interactor faulted: " + sourceMessage.Source, sourceMessage.Content);
            RemoveInteractor(sourceMessage.Source);
        }

        void RemoveInteractor(Interactor interactor)
        {
            Log.DebugFormat("Removing subscriptions for {0}", interactor);

            // Remove the subscriptions
            var subscriptions = new List<string>();
            var emptySubscriptions = new List<string>();
            foreach (var item in _subscriptionCache)
            {
                if (item.Value.Contains(interactor))
                {
                    item.Value.Remove(interactor);
                    if (item.Value.Count == 0)
                        emptySubscriptions.Add(item.Key);
                    subscriptions.Add(item.Key);
                }
            }

            foreach (var topic in subscriptions)
                _messageBus.ForwardedSubscriptionRequests.OnNext(new ForwardedSubscriptionRequest(interactor.Id, topic, false));

            foreach (var topic in emptySubscriptions)
                _subscriptionCache.Remove(topic);
        }

        void HandleUnicastDataMessage(SourceMessage<UnicastDataMessage> sourceMessage)
        {
            ISet<Interactor> subscribers;
            if (_subscriptionCache.TryGetValue(sourceMessage.Content.Topic, out subscribers))
            {
                var subscriber = subscribers.FirstOrDefault(x => x.Id == sourceMessage.Content.ClientId);
                if (subscriber != null)
                    _messageBus.SendableUnicastDataMessages.OnNext(SourceSinkMessage.Create(sourceMessage.Source, subscriber, sourceMessage.Content));
            }
        }

        void HandleMulticastDataMessage(SourceMessage<MulticastDataMessage> sourceMessage)
        {
            ISet<Interactor> subscribers;
            if (_subscriptionCache.TryGetValue(sourceMessage.Content.Topic, out subscribers))
            {
                foreach (var subscriber in subscribers)
                    _messageBus.SendableMulticastDataMessages.OnNext(SourceSinkMessage.Create(sourceMessage.Source, subscriber, sourceMessage.Content));
            }
        }

        void HandleSubscriptionRequest(SourceMessage<SubscriptionRequest> sourceMessage)
        {
            Log.DebugFormat("Received subscription from {0} on \"{1}\"", sourceMessage.Source, sourceMessage.Content);

            if (sourceMessage.Content.IsAdd)
                AddSubscription(sourceMessage.Content.Topic, sourceMessage.Source);
            else
                RemoveSubscription(sourceMessage.Content.Topic, sourceMessage.Source);
        }

        void AddSubscription(string topic, Interactor subscriber)
        {
            // Find the list of interactors that have subscribed to this topic.
            ISet<Interactor> subscribers;
            if (!_subscriptionCache.TryGetValue(topic, out subscribers))
                _subscriptionCache.Add(topic, (subscribers = new HashSet<Interactor>()));

            if (!subscribers.Contains(subscriber))
                subscribers.Add(subscriber);
        }

        void RemoveSubscription(string topic, Interactor subscriber)
        {
            ISet<Interactor> subscribers;
            if (!_subscriptionCache.TryGetValue(topic, out subscribers))
                return;

            if (subscribers.Contains(subscriber))
                subscribers.Remove(subscriber);

            if (subscribers.Count == 0)
                _subscriptionCache.Remove(topic);
        }

        void ForwardSubscriptionRequests(SourceMessage<Regex> sourceMessage)
        {
            foreach (var item in _subscriptionCache.Where(x => sourceMessage.Content.Match(x.Key).Success))
            {
                Log.DebugFormat("Notification pattern {0} matched [{1}] subscribers", sourceMessage.Content, string.Join(",", item.Value));

                foreach (var subscriber in item.Value)
                    sourceMessage.Source.SendMessage(new ForwardedSubscriptionRequest(subscriber.Id, item.Key, true));
            }
        }

        void HandleStalePublisher(SourceMessage<IEnumerable<string>> forwardedMessage)
        {
            foreach (var staleTopic in forwardedMessage.Content)
            {
                ISet<Interactor> subscribers;
                if (_subscriptionCache.TryGetValue(staleTopic, out subscribers))
                {
                    var staleMessage = new MulticastDataMessage(staleTopic, true, null);
                    foreach (var subscriber in subscribers)
                        subscriber.SendMessage(staleMessage);
                }
            }
        }

        public void Dispose()
        {
        }
    }
}

