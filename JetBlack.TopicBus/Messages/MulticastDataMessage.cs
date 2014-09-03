using System;
using System.IO;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Messages
{
    public class MulticastDataMessage : Message
    {
        public readonly string Topic;
        public readonly bool IsImage;
        public readonly object Data;

        public MulticastDataMessage(string topic, bool isImage, object data)
            : base(MessageType.MulticastDataMessage)
        {
            Topic = topic;
            IsImage = isImage;
            Data = data;
        }

        new static public MulticastDataMessage Read(Stream stream, Func<Stream, object> dataReader)
        {
            var topic = stream.ReadString();
            var isImage = stream.ReadBoolean();
            var data = dataReader(stream);
            return new MulticastDataMessage(topic, isImage, data);
        }

        public override Stream Write(Stream stream, Action<Stream, object> dataWriter)
        {
            base.Write(stream, dataWriter);
            stream.Write(Topic);
            stream.Write(IsImage);
            dataWriter(stream, Data);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} [{3}]", MessageType, Topic, IsImage, Data);
        }
    }
}
