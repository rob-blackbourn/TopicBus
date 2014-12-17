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

        static public NotificationRequest ReadBody(Stream stream)
        {
            var topicPattern = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new NotificationRequest(topicPattern, isAdd);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
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
