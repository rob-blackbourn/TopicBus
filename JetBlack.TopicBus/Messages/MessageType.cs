namespace JetBlack.TopicBus.Messages
{
    public enum MessageType : byte
    {
        MulticastDataMessage,
        UnicastDataMessage,
        ForwardedSubscriptionRequest,
        NotificationRequest,
        SubscriptionRequest,
    }
}
