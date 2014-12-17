namespace JetBlack.TopicBus.Messages
{
    public enum MessageType : byte
    {
        MulticastData,
        UnicastData,
        ForwardedSubscriptionRequest,
        NotificationRequest,
        SubscriptionRequest
    }
}
