using System.Runtime.Serialization.Formatters.Binary;

namespace JetBlack.TopicBus.IO
{
    public class BinarySerializer : ISerializer
    {
        static readonly BinaryFormatter BinaryFormatter = new BinaryFormatter();

        public object Deserialize(System.IO.Stream stream)
        {
            return BinaryFormatter.Deserialize(stream);
        }

        public void Serialize(System.IO.Stream stream, object obj)
        {
            BinaryFormatter.Serialize(stream, obj);
        }
    }
}

