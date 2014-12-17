namespace JetBlack.TopicBus.IO
{
    public interface ISerializer
    {
        object Deserialize(byte[] bytes);
        byte[] Serialize(object obj);
    }
}

