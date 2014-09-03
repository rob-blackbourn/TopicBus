﻿namespace JetBlack.TopicBus.Distributor
{
    public static class SourceMessage
    {
        public static SourceMessage<T> Create<T>(Interactor source, T content)
        {
            return new SourceMessage<T>(source, content);
        }
    }

    public class SourceMessage<T>
    {
        public Interactor Source { get; private set; }
        public T Content { get; private set; }

        public SourceMessage(Interactor source, T content)
        {
            Source = source;
            Content = content;
        }
    }
}
