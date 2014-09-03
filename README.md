TopicBus
========

This is a real time message bus sharing similarities with products such as "Tibco Rendezvous (TM)",
"Reuters SSL (TM)" and "Bloomberg API (TM)".

My main motivation was to bring together features from each of the above products.

It was written in C# using Xamarin Studio on a Mac.

Under the "Examples" folder are a number of console applications. The TopicBusConsole must be running
for any of the other examples to work.

To start I suggest:

1.  Build all!
2.  Run the TopicBusConsole.
3.  Run the TopicBusSubscriber
4.  Run the TopicBusCachingPublisher.
5.  Note the published messages appearing in the subscriber.
