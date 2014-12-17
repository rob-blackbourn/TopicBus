using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using JetBlack.TopicBus.Messages;
using System.Reactive.Concurrency;
using log4net;

namespace JetBlack.TopicBus.Distributor
{
    class Market : IDisposable
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Market));

        readonly IList<Interactor> _interactors = new List<Interactor>();
        readonly IDisposable _listenerDisposable;
        readonly MessageBus _messageBus = new MessageBus();
        readonly SubscriptionManager _subscriptionManager;
        readonly PublisherManager _publisherManager;
        readonly NotificationManager _notificationManager;

        public Market(IObservable<Interactor> listenerObservable)
        {
            _subscriptionManager = new SubscriptionManager(_messageBus);
            _publisherManager = new PublisherManager(_messageBus);
            _notificationManager = new NotificationManager(_messageBus);

            var scheduler = new EventLoopScheduler();

            _listenerDisposable = listenerObservable.SubscribeOn(scheduler).Subscribe(AddInteractor);
            _messageBus.ClosedInteractors.SubscribeOn(scheduler).Subscribe(RemoveInteractor);
            _messageBus.FaultedInteractors.SubscribeOn(scheduler).Subscribe(FaultedInteractor);
        }

        void AddInteractor(Interactor interactor)
        {
            _interactors.Add(interactor);

            interactor.ToObservable()
                .Subscribe(
                    message => Forward(message, interactor),
                    error => _messageBus.FaultedInteractors.OnNext(new SourceMessage<Exception>(interactor, error)),
                    () => _messageBus.ClosedInteractors.OnNext(interactor));
        }

        void RemoveInteractor(Interactor interactor)
        {
            _interactors.Remove(interactor);
        }

        void FaultedInteractor(SourceMessage<Exception> sourceMessage)
        {
            Log.Warn("Interactor faulted: " + sourceMessage.Source, sourceMessage.Content);

            RemoveInteractor(sourceMessage.Source);
        }

        void Forward(Message message, Interactor sender)
        {
            switch (message.MessageType)
            {
                case MessageType.SubscriptionRequest:
                    {
                        var subscriptionRequest = (SubscriptionRequest)message;
                        _messageBus.SubscriptionRequests.OnNext(SourceMessage.Create(sender, subscriptionRequest));
                        _messageBus.ForwardedSubscriptionRequests.OnNext(new ForwardedSubscriptionRequest(sender.Id, subscriptionRequest.Topic, subscriptionRequest.IsAdd));
                    }
                    break;

                case MessageType.MulticastDataMessage:
                    _messageBus.PublishedMulticastDataMessages.OnNext(SourceMessage.Create(sender, (MulticastDataMessage)message));
                    break;

                case MessageType.UnicastDataMessage:
                    _messageBus.PublishedUnicastDataMessages.OnNext(SourceMessage.Create(sender, (UnicastDataMessage)message));
                    break;

                case MessageType.NotificationRequest:
                    _messageBus.NotificationRequests.OnNext(SourceMessage.Create(sender, (NotificationRequest)message));
                    break;

                default:
                    throw new ArgumentException("invalid message type");
            }
        }

        public void Dispose()
        {
            _listenerDisposable.Dispose();

            foreach (var interactor in _interactors)
                interactor.Dispose();

            _subscriptionManager.Dispose();
            _publisherManager.Dispose();
            _notificationManager.Dispose();
        }
    }
}