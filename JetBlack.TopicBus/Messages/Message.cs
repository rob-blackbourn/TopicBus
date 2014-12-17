using System.IO;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Messages
{
    public abstract class Message
    {
        public readonly MessageType MessageType;

        protected Message(MessageType messageType)
        {
            MessageType = messageType;
        }

        public static Message Read(Stream stream)
        {
            var messageType = (MessageType)stream.ReadByte();

            switch (messageType)
            {
                case MessageType.MulticastData:
                    return MulticastData.Read(stream);
                case MessageType.UnicastData:
                    return UnicastData.Read(stream);
                case MessageType.ForwardedSubscriptionRequest:
                    return ForwardedSubscriptionRequest.Read(stream);
                case MessageType.NotificationRequest:
                    return NotificationRequest.Read(stream);
                case MessageType.SubscriptionRequest:
                    return SubscriptionRequest.Read(stream);
                default:
                    throw new InvalidDataException("unknown message type");
            }
        }

        public virtual Stream Write(Stream stream)
        {
            stream.Write((byte)MessageType);
            return stream;
        }
    }
}

