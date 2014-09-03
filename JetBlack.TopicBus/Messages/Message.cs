using System;
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

        public static Message Read(Stream stream, Func<Stream, object> dataReader)
        {
            var messageType = (MessageType)stream.ReadByte();

            switch (messageType)
            {
                case MessageType.MulticastDataMessage:
                    return MulticastDataMessage.Read(stream, dataReader);
                case MessageType.UnicastDataMessage:
                    return UnicastDataMessage.Read(stream, dataReader);
                case MessageType.ForwardedSubscriptionRequest:
                    return ForwardedSubscriptionRequest.Read(stream, dataReader);
                case MessageType.NotificationRequest:
                    return NotificationRequest.Read(stream, dataReader);
                case MessageType.SubscriptionRequest:
                    return SubscriptionRequest.Read(stream, dataReader);
                default:
                    throw new InvalidDataException("unknown message type");
            }
        }

        public virtual Stream Write(Stream stream, Action<Stream, object> dataWriter)
        {
            stream.Write((byte)MessageType);
            return stream;
        }
    }
}

