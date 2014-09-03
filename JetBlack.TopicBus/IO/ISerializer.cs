using System.IO;

namespace JetBlack.TopicBus.IO
{
    public interface ISerializer
    {
        object Deserialize(Stream stream);
        void Serialize(Stream stream, object obj);
    }
}

