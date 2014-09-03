using System;
using System.IO;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Messages
{
    public class SubscriptionRequest : Message
    {
        public readonly string Topic;
        public readonly bool IsAdd;

        public SubscriptionRequest(string topic, bool isAdd)
            : base(MessageType.SubscriptionRequest)
        {
            Topic = topic;
            IsAdd = isAdd;
        }

        new static public SubscriptionRequest Read(Stream stream, Func<Stream, object> dataReader)
        {
            var topic = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new SubscriptionRequest(topic, isAdd);
        }

        public override Stream Write(Stream stream, Action<Stream,object> dataWriter)
        {
            base.Write(stream, dataWriter);
            stream.Write(Topic);
            stream.Write(IsAdd);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", MessageType, Topic, IsAdd);
        }
    }
}
