using System;
using System.IO;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Messages
{
    public class NotificationRequest : Message
    {
        public readonly bool IsAdd;
        public readonly string TopicPattern;

        public NotificationRequest(string topicPattern, bool isAdd)
            : base(MessageType.NotificationRequest)
        {
            TopicPattern = topicPattern;
            IsAdd = isAdd;
        }

        new static public NotificationRequest Read(Stream stream, Func<Stream, object> dataReader)
        {
            var topicPattern = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new NotificationRequest(topicPattern, isAdd);
        }

        public override Stream Write(Stream stream, Action<Stream, object> dataWriter)
        {
            base.Write(stream, dataWriter);
            stream.Write(TopicPattern);
            stream.Write(IsAdd);
            return stream;
        }

        override public string ToString()
        {
            return string.Format("{0} {1} {2}", MessageType, TopicPattern, IsAdd);
        }
    }
}
