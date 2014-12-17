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

        public readonly ISubject<SourceMessage<MulticastDataMessage>> PublishedMulticastDataMessages = new Subject<SourceMessage<MulticastDataMessage>>();
        public readonly ISubject<SourceMessage<UnicastDataMessage>> PublishedUnicastDataMessages = new Subject<SourceMessage<UnicastDataMessage>>();
        public readonly ISubject<SourceSinkMessage<MulticastDataMessage>> SendableMulticastDataMessages = new Subject<SourceSinkMessage<MulticastDataMessage>>();
        public readonly ISubject<SourceSinkMessage<UnicastDataMessage>> SendableUnicastDataMessages = new Subject<SourceSinkMessage<UnicastDataMessage>>();

        public readonly ISubject<SourceMessage<IEnumerable<string>>> StalePublishers = new Subject<SourceMessage<IEnumerable<string>>>();

        public readonly ISubject<Interactor> ClosedInteractors = new Subject<Interactor>();
        public readonly ISubject<SourceMessage<Exception>> FaultedInteractors = new Subject<SourceMessage<Exception>>();
    }
}

