using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace JetBlack.TopicBus.IO
{
    public class BinaryEncoder : IByteEncoder
    {
        static readonly BinaryFormatter BinaryFormatter = new BinaryFormatter();

        public object Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length <= 0)
                return null;

            using (var stream = new MemoryStream(bytes))
            {
                return BinaryFormatter.Deserialize(stream);
            }
        }

        public byte[] Encode(object obj)
        {
            using (var stream = new MemoryStream())
            {
                BinaryFormatter.Serialize(stream, obj);
                stream.Flush();
                return stream.GetBuffer();
            }
        }
    }
}

