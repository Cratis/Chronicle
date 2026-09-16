// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_an_event;

public class and_generation_hashes_are_provided : given.an_event_sequence_storage
{
    AppendedEvent _acknowledged;
    AppendedEvent _readBack;

    async Task Because()
    {
        var result = await _storage.Append(
            EventSequenceNumber.First,
            EventSourceType.Default,
            "source",
            EventStreamType.All,
            EventStreamId.Default,
            new EventType(_eventType.Id, 2),
            CorrelationId.NotSet,
            [],
            [],
            [],
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = new(), [(EventTypeGeneration)2] = new() },
            new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = "first-hash", [(EventTypeGeneration)2] = "second-hash" });
        _acknowledged = result.AsT0;
        _readBack = await _storage.GetEventAt(EventSequenceNumber.First);
    }

    [Fact] void should_acknowledge_the_appended_generations_hash() => _acknowledged.Context.Hash.ShouldEqual((EventHash)"second-hash");
    [Fact] void should_preserve_the_hash_on_read_back() => _readBack.Context.Hash.ShouldEqual(_acknowledged.Context.Hash);
}
