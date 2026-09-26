// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_an_event_has_multiple_generations : given.an_event_sequence_storage
{
    AppendedEvent _acknowledged;

    async Task Because()
    {
        var original = new ExpandoObject();
        ((IDictionary<string, object?>)original)["legacyValue"] = "original";
        var upcast = new ExpandoObject();
        ((IDictionary<string, object?>)upcast)["value"] = "migrated";
        var @event = new EventToAppendToStorage(EventSequenceNumber.First, EventSourceType.Default, "some-source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, original, "original-hash")
        {
            GenerationalContent = new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = original, [new EventTypeGeneration(2)] = upcast },
            ContentHashes = new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = "original-hash", [new EventTypeGeneration(2)] = "migrated-hash" }
        };
        _acknowledged = (await _storage.AppendMany([@event])).AsT0.Single();
    }

    [Fact] void should_store_both_generations() => _storage.Events.Single().GenerationalContent.Keys.ShouldContainOnly([1, 2]);
    [Fact] void should_store_the_upcast_content() => _acknowledged.GenerationalContent[2].ShouldContain("migrated");
    [Fact] void should_keep_the_appended_generation_hash() => _acknowledged.Context.Hash.Value.ShouldEqual("original-hash");
}
