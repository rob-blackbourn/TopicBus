using System.IO;
using System.Collections.Generic;

namespace JetBlack.TopicBus.IO
{
    public class SimpleSerializer : ISerializer
    {
        public object Deserialize(Stream stream)
        {
            return stream.ReadDictionary();
        }

        public void Serialize(Stream stream, object obj)
        {
            stream.Write((IDictionary<string,object>)obj);
        }
    }
}

