using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace JetBlack.TopicBus.IO
{
    public class JsonEncoder : IByteEncoder
    {
        public object Decode(byte[] bytes)
        {
            var s = Encoding.UTF8.GetString(bytes);
            var obj = JsonConvert.DeserializeObject<Dictionary<string,object>>(s);
            return obj;
        }

        public byte[] Encode(object obj)
        {
            var s = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(s);
            return bytes;
        }
    }
}

