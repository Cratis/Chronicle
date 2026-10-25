// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreNamespaceStorage.when_getting_observer_keys;

/// <summary>
/// A reactor registering behind the tail is caught up from these keys (#4679).
/// An empty enumeration means zero catch-up steps and leaves the missed events undelivered.
/// </summary>
public class and_events_were_appended_from_the_position : given.a_namespace_storage
{
    static readonly EventType _typeA = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    static readonly EventType _typeB = new("22222222-2222-2222-2222-222222222222", EventTypeGeneration.First);
    List<Key> _keys;

    async Task Establish()
    {
        await Append(0, "behind", _typeA);
        await Append(1, "first", _typeA);
        await Append(2, "other-type", _typeB);
        await Append(3, "first", _typeA);
    }

    async Task Because()
    {
        var index = await _storage.ObserverKeyIndexes.GetFor(
            new ObserverKey("reactor", "some-store", "some-namespace", EventSequenceId.Log),
            ObserverReaderScope.Explicit([_typeA]));
        _keys = [];
        await foreach (var key in index.GetKeys(1UL).WithCancellation(TestContext.Current.CancellationToken))
        {
            _keys.Add(key);
        }
    }

    [Fact] void should_enumerate_the_event_source_with_an_event_from_the_position() => _keys.ShouldContain((Key)"first");
    [Fact] void should_enumerate_each_event_source_once() => _keys.Count.ShouldEqual(1);
    [Fact] void should_not_enumerate_an_event_source_only_behind_the_position() => _keys.ShouldNotContain((Key)"behind");
    [Fact] void should_not_enumerate_an_event_source_of_another_event_type() => _keys.ShouldNotContain((Key)"other-type");

    Task Append(ulong sequenceNumber, EventSourceId eventSourceId, EventType eventType) =>
        _storage.GetEventSequence(EventSequenceId.Log).Append(
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
}
