namespace JetBlack.TopicBus.IO
{
    public interface IByteEncoder
    {
        object Decode(byte[] bytes);
        byte[] Encode(object obj);
    }
}

