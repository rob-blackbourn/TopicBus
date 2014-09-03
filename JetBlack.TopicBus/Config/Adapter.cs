using JetBlack.TopicBus.IO;

namespace JetBlack.TopicBus.Config
{
    public class Adapter
    {
        public string Name;

        public int Port;

        public ISerializer Serializer;

        public Adapter(string name, int port, ISerializer serializer)
        {
            Name = name;
            Port = port;
            Serializer = serializer;
        }
    }
}

