using System;
using System.IO;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Messages
{
    public class ForwardedSubscriptionRequest : Message
    {
        public readonly int ClientId;
        public readonly string Topic;
        public readonly bool IsAdd;

        public ForwardedSubscriptionRequest(int clientId, string topic, bool isAdd)
            : base(MessageType.ForwardedSubscriptionRequest)
        {
            ClientId = clientId;
            Topic = topic;
            IsAdd = isAdd;
        }

        new static public ForwardedSubscriptionRequest Read(Stream stream, Func<Stream, object> dataReader)
        {
            var clientId = stream.ReadInt32();
            var topic = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new ForwardedSubscriptionRequest(clientId, topic, isAdd);
        }

        public override Stream Write(Stream stream, Action<Stream,object> dataWriter)
        {
            base.Write(stream, dataWriter);
            stream.Write(ClientId);
            stream.Write(Topic);
            stream.Write(IsAdd);
            return stream;
        }

        override public string ToString()
        {
            return string.Format("{0} {1} {2} {3}", MessageType, ClientId, Topic, IsAdd);
        }
    }
}
