using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using JetBlack.TopicBus.Messages;
using log4net;

namespace JetBlack.TopicBus.Distributor
{
    public class NotificationManager : IDisposable
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(SubscriptionManager));

        readonly Dictionary<string, Notification> _cache = new Dictionary<string, Notification>();
        readonly MessageBus _messageBus;

        public NotificationManager(MessageBus messageBus)
        {
            _messageBus = messageBus;

            var scheduler = new EventLoopScheduler();

            _messageBus.NotificationRequests.SubscribeOn(scheduler).Subscribe(HandleNotificationRequest);
            _messageBus.ForwardedSubscriptionRequests.SubscribeOn(scheduler).Subscribe(NotifyListenersOnTopic);
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
            Log.DebugFormat("Removing notification requests from {0}", interactor);

            var emptyNotifications = new List<string>();
            foreach (var item in _cache)
            {
                if (item.Value.Notifiables.Contains(interactor))
                {
                    item.Value.Notifiables.Remove(interactor);
                    if (item.Value.Notifiables.Count == 0)
                        emptyNotifications.Add(item.Key);
                }
            }

            foreach (var topic in emptyNotifications)
                _cache.Remove(topic);
        }

        void HandleNotificationRequest(SourceMessage<NotificationRequest> sourceMessage)
        {
            Log.DebugFormat("Handling notification request for {0} on {1}", sourceMessage.Source, sourceMessage.Content);

            if (sourceMessage.Content.IsAdd)
                AddNotificationRequest(sourceMessage.Source, sourceMessage.Content);
            else
                RemoveNotificationRequest(sourceMessage.Source, sourceMessage.Content);
        }

        void AddNotificationRequest(Interactor source, NotificationRequest notificationRequest)
        {
            Notification notification;
            if (!_cache.TryGetValue(notificationRequest.TopicPattern, out notification))
                _cache.Add(notificationRequest.TopicPattern, notification = new Notification(new Regex(notificationRequest.TopicPattern)));

            if (!notification.Notifiables.Contains(source))
            {
                notification.Notifiables.Add(source);
                _messageBus.NewNotificationRequests.OnNext(SourceMessage.Create(source, notification.TopicPattern));
            }
        }

        public void RemoveNotificationRequest(Interactor source, NotificationRequest notificationRequest)
        {
            Notification notification;
            if (_cache.TryGetValue(notificationRequest.TopicPattern, out notification) && notification.Notifiables.Contains(source))
            {
                notification.Notifiables.Remove(source);
                if (notification.Notifiables.Count == 0)
                    _cache.Remove(notificationRequest.TopicPattern);
            }
        }

        void NotifyListenersOnTopic(ForwardedSubscriptionRequest message)
        {
            var notifiables = _cache.Values
                .Where(x => x.TopicPattern.Matches(message.Topic).Count != 0)
                .SelectMany(x => x.Notifiables)
                .ToList();

            Log.DebugFormat("Notifying interactors[{0}] of subscription {1}", string.Join(",", notifiables), message);

            foreach (var notifiable in notifiables)
                notifiable.SendMessage(message);
        }

        public void Dispose()
        {
        }

        private class Notification
        {
            internal readonly ISet<Interactor> Notifiables = new HashSet<Interactor>();
            internal readonly Regex TopicPattern;

            internal Notification(Regex topicPattern)
            {
                TopicPattern = topicPattern;
            }
        }
    }
}
