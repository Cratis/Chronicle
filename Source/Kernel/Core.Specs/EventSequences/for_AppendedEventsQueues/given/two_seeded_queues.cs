// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.ObserverRouting;
using Cratis.Chronicle.Storage.Observation;
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

        // The routing table holds one observer of the subscribed event type on every queue, so the router narrows
        // delivery to the event types the table knows and nothing is broadcast for the others.
        var definitions = Enumerable.Range(0, QueueCount)
            .Select(queueIndex => new ObserverDefinition(ObserverIdOnQueue(queueIndex), EventTypeSet.From([subscribed_event_type]), EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true))
            .ToFrozenDictionary(_ => _.Identifier);
        _silo.AddService<IObservers>(new FixedObservers(ObserverRoutingTable.From(new ObserverDefinitionRegistrySnapshot(1, definitions))));

        _queueGrains = new IAppendedEventsQueue[QueueCount];
        var queueGrainsByIdentity = new Dictionary<string, IAppendedEventsQueue>();
        for (var queueIndex = 0; queueIndex < QueueCount; queueIndex++)
        {
            var queueGrain = Substitute.For<IAppendedEventsQueue>();
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

    static ObserverId ObserverIdOnQueue(int queueIndex)
    {
        var router = new AppendedEventsQueueRouter(QueueCount);
        return Enumerable.Range(0, 1000)
            .Select(_ => new ObserverId($"observer-{_}"))
            .First(_ => router.GetQueueIndexFor(_) == queueIndex);
    }

    /// <summary>
    /// An <see cref="IObservers"/> that always answers with the same table.
    /// </summary>
    /// <param name="table">The table to answer with.</param>
    sealed class FixedObservers(ObserverRoutingTable table) : IObservers
    {
        public ValueTask<ObserverRoutingTable> GetTable(EventStoreName eventStore) => ValueTask.FromResult(table);

        public Task<ObserverDefinitionRevision> Acknowledge(EventStoreName eventStore, ObserverId observerId, ObserverDefinitionRevision revision) =>
            Task.FromResult(revision);
    }
}
