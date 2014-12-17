using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using JetBlack.TopicBus.Messages;
using log4net;

namespace JetBlack.TopicBus.Distributor
{
    class PublisherManager : IDisposable
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(PublisherManager));

        readonly Dictionary<string, ISet<Interactor>> _topicToPublisherMap = new Dictionary<string, ISet<Interactor>>();
        readonly Dictionary<Interactor, ISet<string>> _publisherToTopicMap = new Dictionary<Interactor, ISet<string>>();
        readonly MessageBus _messageBus;

        public PublisherManager(MessageBus messageBus)
        {
            _messageBus = messageBus;

            var scheduler = new EventLoopScheduler();

            _messageBus.SendableMulticastDataMessages.SubscribeOn(scheduler).Subscribe(PublishMulticastMessage);
            _messageBus.SendableUnicastDataMessages.SubscribeOn(scheduler).Subscribe(PublishUnicastMessage);
            _messageBus.ClosedInteractors.SubscribeOn(scheduler).Subscribe(RemoveInteractor);
            _messageBus.FaultedInteractors.SubscribeOn(scheduler).Subscribe(FaultedInteractor);
        }

        void FaultedInteractor(SourceMessage<Exception> sourceMessage)
        {
            Log.Warn("Interactor faulted: " + sourceMessage.Source, sourceMessage.Content);
            RemoveInteractor(sourceMessage.Source);
        }

        void PublishUnicastMessage(SourceSinkMessage<UnicastDataMessage> routedMessage)
        {
            RememberPublisherForTopic(routedMessage.Content.Topic, routedMessage.Source);
            routedMessage.Sink.SendMessage(routedMessage.Content);
        }

        void PublishMulticastMessage(SourceSinkMessage<MulticastDataMessage> routedMessage)
        {
            RememberPublisherForTopic(routedMessage.Content.Topic, routedMessage.Source);
            routedMessage.Sink.SendMessage(routedMessage.Content);
        }

        void RememberPublisherForTopic(string topic, Interactor publisher)
        {
            ISet<Interactor> publishers;
            if (!_topicToPublisherMap.TryGetValue(topic, out publishers))
                _topicToPublisherMap.Add(topic, publishers = new HashSet<Interactor>());
            if (!publishers.Contains(publisher))
                publishers.Add(publisher);

            ISet<string> topics;
            if (!_publisherToTopicMap.TryGetValue(publisher, out topics))
                _publisherToTopicMap.Add(publisher, topics = new HashSet<string>());
            if (!topics.Contains(topic))
                topics.Add(topic);
        }

        void RemoveInteractor(Interactor interactor)
        {
            var staleTopics = new HashSet<string>();

            ISet<string> topics;
            if (_publisherToTopicMap.TryGetValue(interactor, out topics))
            {
                foreach (var topic in topics)
                {
                    ISet<Interactor> publishers;
                    if (_topicToPublisherMap.TryGetValue(topic, out publishers) && publishers.Contains(interactor))
                    {
                        publishers.Remove(interactor);
                        if (publishers.Count == 0)
                        {
                            _topicToPublisherMap.Remove(topic);
                            staleTopics.Add(topic);
                        }
                    }
                }

                _publisherToTopicMap.Remove(interactor);
            }

            _messageBus.StalePublishers.OnNext(SourceMessage.Create<IEnumerable<string>>(interactor, staleTopics));
        }

        public void Dispose()
        {
        }
    }
}

