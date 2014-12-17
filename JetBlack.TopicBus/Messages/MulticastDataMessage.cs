using System;
using System.IO;
using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Messages
{
    public class MulticastDataMessage : Message
    {
        public readonly string Topic;
        public readonly bool IsImage;
        public readonly byte[] Data;

        public MulticastDataMessage(string topic, bool isImage, byte[] data)
            : base(MessageType.MulticastDataMessage)
        {
            Topic = topic;
            IsImage = isImage;
            Data = data;
        }

        new static public MulticastDataMessage Read(Stream stream)
        {
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
            return new MulticastDataMessage(topic, isImage, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
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
            return string.Format("{0} {1} {2} [{3}]", MessageType, Topic, IsImage, Data);
        }
    }
}
