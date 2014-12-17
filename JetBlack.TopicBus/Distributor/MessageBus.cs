using System;
using System.Reactive.Subjects;
using JetBlack.TopicBus.Messages;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace JetBlack.TopicBus.Distributor
{
    class MessageBus
    {
        public readonly ISubject<SourceMessage<SubscriptionRequest>> SubscriptionRequests = new Subject<SourceMessage<SubscriptionRequest>>();
        public readonly ISubject<ForwardedSubscriptionRequest> ForwardedSubscriptionRequests = new Subject<ForwardedSubscriptionRequest>();

        public readonly ISubject<SourceMessage<NotificationRequest>> NotificationRequests = new Subject<SourceMessage<NotificationRequest>>();
        public readonly ISubject<SourceMessage<Regex>> NewNotificationRequests = new Subject<SourceMessage<Regex>>();

        public readonly ISubject<SourceMessage<MulticastData>> PublishedMulticastDataMessages = new Subject<SourceMessage<MulticastData>>();
        public readonly ISubject<SourceMessage<UnicastData>> PublishedUnicastDataMessages = new Subject<SourceMessage<UnicastData>>();
        public readonly ISubject<SourceSinkMessage<MulticastData>> SendableMulticastDataMessages = new Subject<SourceSinkMessage<MulticastData>>();
        public readonly ISubject<SourceSinkMessage<UnicastData>> SendableUnicastDataMessages = new Subject<SourceSinkMessage<UnicastData>>();

        public readonly ISubject<SourceMessage<IEnumerable<string>>> StalePublishers = new Subject<SourceMessage<IEnumerable<string>>>();

        public readonly ISubject<Interactor> ClosedInteractors = new Subject<Interactor>();
        public readonly ISubject<SourceMessage<Exception>> FaultedInteractors = new Subject<SourceMessage<Exception>>();
    }
}

