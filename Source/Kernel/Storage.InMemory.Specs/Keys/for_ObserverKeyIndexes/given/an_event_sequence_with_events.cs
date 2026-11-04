// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Storage.InMemory.Keys.for_ObserverKeyIndexes.given;

public class an_event_sequence_with_events : Specification
{
    protected static readonly EventType _typeA = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    protected static readonly EventType _typeB = new("22222222-2222-2222-2222-222222222222", EventTypeGeneration.First);
    protected static readonly ObserverKey _observerKey = new("reactor", "some-store", "some-namespace", EventSequenceId.Log);
    protected EventSequenceStorage _eventSequence;
    protected ObserverKeyIndexes _indexes;
    protected List<Key> _keys;

    async Task Establish()
    {
        _eventSequence = new EventSequenceStorage("some-store", "some-namespace", EventSequenceId.Log, new IdentityStorage());
        _indexes = new ObserverKeyIndexes(_ => _eventSequence);
        await Append(0, "behind", _typeA);
        await Append(1, "first", _typeA);
        await Append(2, "other-type", _typeB);
        await Append(3, "first", _typeA);
    }

    protected Task Append(ulong sequenceNumber, EventSourceId eventSourceId, EventType eventType) =>
        _eventSequence.Append(
            sequenceNumber,
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            eventType,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { { EventTypeGeneration.First, new ExpandoObject() } },
            new Dictionary<EventTypeGeneration, EventHash>());

    protected static async Task<List<Key>> Enumerate(IObserverKeys keys)
    {
        var result = new List<Key>();
        await foreach (var key in keys.WithCancellation(TestContext.Current.CancellationToken))
        {
            result.Add(key);
        }

        return result;
    }
}
