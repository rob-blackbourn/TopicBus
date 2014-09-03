using System;
using System.IO;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Messages
{
    public class UnicastDataMessage : Message
    {
        public readonly int ClientId;
        public readonly string Topic;
        public readonly bool IsImage;
        public readonly object Data;

        public UnicastDataMessage(int clientId, string topic, bool isImage, object data)
            : base(MessageType.UnicastDataMessage)
        {
            ClientId = clientId;
            Topic = topic;
            IsImage = isImage;
            Data = data;
        }

        new static public UnicastDataMessage Read(Stream stream, Func<Stream,object> dataReader)
        {
            var clientId = stream.ReadInt32();
            var topic = stream.ReadString();
            var isImage = stream.ReadBoolean();
            var data = dataReader(stream);
            return new UnicastDataMessage(clientId, topic, isImage, data);
        }

        public override Stream Write(Stream stream, Action<Stream,object> dataWriter)
        {
            base.Write(stream, dataWriter);
            stream.Write(ClientId);
            stream.Write(Topic);
            stream.Write(IsImage);
            dataWriter(stream, Data);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} [{4}]", MessageType, ClientId, Topic, IsImage, Data);
        }
    }
}

