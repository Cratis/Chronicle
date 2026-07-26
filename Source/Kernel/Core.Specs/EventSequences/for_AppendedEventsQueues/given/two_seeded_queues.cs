// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueues.given;

public class two_seeded_queues : Specification
{
    protected const int QueueCount = 2;

    protected static readonly EventType subscribed_event_type = new("some-subscribed-event", EventTypeGeneration.First);
    protected static readonly EventType unsubscribed_event_type = new("some-unsubscribed-event", EventTypeGeneration.First);

    protected TestKitSilo _silo = new();
    protected AppendedEventsQueues _queues;
    protected IAppendedEventsQueue[] _queueGrains;
    protected EventSequenceKey _eventSequenceKey;

    async Task Establish()
    {
        _eventSequenceKey = new EventSequenceKey(EventSequenceId.Log, "some-event-store", "some-namespace");

        _silo.AddService<IOptions<ChronicleOptions>>(Options.Create(new ChronicleOptions
        {
            Events = new Configuration.Events { Queues = QueueCount }
        }));

        // Every queue is seeded with an authoritative subscription that covers only the subscribed event type,
        // so the router narrows delivery and nothing is broadcast merely because a queue is unseeded.
        _queueGrains = new IAppendedEventsQueue[QueueCount];
        var queueGrainsByIdentity = new Dictionary<string, IAppendedEventsQueue>();
        for (var queueIndex = 0; queueIndex < QueueCount; queueIndex++)
        {
            var queueGrain = Substitute.For<IAppendedEventsQueue>();
            queueGrain.GetSubscriptions().Returns(
            [
                new AppendedEventsQueueObserverSubscription(ObserverKeyFor($"observer-{queueIndex}"), [subscribed_event_type.Id])
            ]);

            _queueGrains[queueIndex] = queueGrain;
            queueGrainsByIdentity[GrainIdKeyExtensions.CreateIntegerKey(queueIndex, _eventSequenceKey).ToString()] = queueGrain;
        }

        _silo.AddProbe<IAppendedEventsQueue>(identity => queueGrainsByIdentity[identity.ToString()]);

        _queues = await _silo.CreateGrainAsync<AppendedEventsQueues>(_eventSequenceKey.ToString());
    }

    protected ObserverKey ObserverKeyFor(string observerId) =>
        new(observerId, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceKey.EventSequenceId);

    /// <summary>
    /// Resolves the queue an observer routes to through the same router the grain uses, so the specs stay correct
    /// whatever the hash produces rather than hard-coding an index.
    /// </summary>
    /// <param name="observerKey"><see cref="ObserverKey"/> to resolve the queue for.</param>
    /// <returns>The zero-based queue index.</returns>
    protected static int QueueIndexFor(ObserverKey observerKey) =>
        new AppendedEventsQueueRouter(QueueCount).GetQueueIndexFor(observerKey);

    protected static AppendedEvent EventOfType(EventTypeId eventTypeId) => AppendedEvent.Empty() with
    {
        Context = EventContext.Empty with
        {
            EventType = new EventType(eventTypeId, EventTypeGeneration.First),
            EventSourceId = "some-event-source"
        }
    };
}
