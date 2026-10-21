// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_appended_generation_is_recorded(ReplicaSetMongoDBFixture fixture) : given.a_storage_for_appended_generation(fixture)
{
    Event _stored;
    AppendedEvent _read;
    AppendedEvent _acknowledged;

    async Task Because()
    {
        var entry = GenerationalEvent(EventSequenceNumber.First, 1) with
        {
            ContentHashes = new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = "original-hash", [new EventTypeGeneration(2)] = "migrated-hash" }
        };
        _acknowledged = (await _storage.Append(entry.SequenceNumber, entry.EventSourceType, entry.EventSourceId, entry.EventStreamType, entry.EventStreamId, entry.EventType, entry.CorrelationId, entry.Causation, entry.CausedByChain, entry.Tags, entry.Occurred, entry.GenerationalContent, entry.ContentHashes)).AsT0;
        _stored = await Stored(entry.SequenceNumber);
        _read = await _storage.GetEventAt(entry.SequenceNumber);
    }

    [Fact] void should_expose_the_appended_generation_live() => _acknowledged.Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_expose_the_appended_generation_on_read() => _read.Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_expose_generation_hashes_live() => _acknowledged.GenerationalHashes.Keys.ShouldContainOnly(1, 2);
    [Fact] void should_expose_generation_hashes_on_read() => _read.GenerationalHashes.Keys.ShouldContainOnly(1, 2);
    [Fact] void should_preserve_the_original_generations_hash() => _acknowledged.GenerationalHashes[1].Value.ShouldEqual("original-hash");
    [Fact] void should_preserve_the_migrated_generations_hash() => _read.GenerationalHashes[2].Value.ShouldEqual("migrated-hash");
    [Fact] void should_record_the_original_generation() => _stored.AppendedGeneration.ShouldEqual((uint?)1);
    [Fact] void should_still_acknowledge_the_appended_generation() => _acknowledged.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_still_read_the_highest_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_still_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
}
