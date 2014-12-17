namespace JetBlack.TopicBus.Distributor
{
    static class SourceSinkMessage
    {
        public static SourceSinkMessage<T> Create<T>(Interactor source, Interactor sink, T content)
        {
            return new SourceSinkMessage<T>(source, sink, content);
        }
    }

    class SourceSinkMessage<T>
    {
        public Interactor Source { get; private set; }
        public Interactor Sink { get; private set; }
        public T Content { get; private set; }

        public SourceSinkMessage(Interactor source, Interactor sink, T content)
        {
            Source = source;
            Sink = sink;
            Content = content;
        }
    }
}

