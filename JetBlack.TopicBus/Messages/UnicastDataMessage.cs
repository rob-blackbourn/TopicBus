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
        public readonly byte[] Data;

        public UnicastDataMessage(int clientId, string topic, bool isImage, byte[] data)
            : base(MessageType.UnicastDataMessage)
        {
            ClientId = clientId;
            Topic = topic;
            IsImage = isImage;
            Data = data;
        }

        new static public UnicastDataMessage Read(Stream stream)
        {
            var clientId = stream.ReadInt32();
            var topic = stream.ReadString();
            var isImage = stream.ReadBoolean();
            var nbytes = stream.ReadInt32();
            var data = new byte[nbytes];
            for (int i = 0; i < nbytes; ++i)
            {
                var b = stream.ReadByte();
                if (b == -1)
                    throw new EndOfStreamException();
                data[i] = (byte)b;
            }
            return new UnicastDataMessage(clientId, topic, isImage, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(ClientId);
            stream.Write(Topic);
            stream.Write(IsImage);
            var data = Data ?? new byte[0];
            stream.Write(data.Length);
            for (int i = 0; i < data.Length; ++i)
                stream.Write(data[i]);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} [{4}]", MessageType, ClientId, Topic, IsImage, Data);
        }
    }
}

