// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading;

public class and_several_generations_were_stored_at_append : given.a_storage_for_appended_generation
{
    AppendedEvent _read;

    async Task Establish()
    {
        var entry = EventAt(EventSequenceNumber.First, 1);
        (await _storage.Append(entry.SequenceNumber, entry.EventSourceType, entry.EventSourceId, entry.EventStreamType, entry.EventStreamId, entry.EventType, entry.CorrelationId, entry.Causation, entry.CausedByChain, entry.Tags, entry.Occurred, entry.GenerationalContent, entry.ContentHashes)).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => _read = await _storage.GetEventAt(EventSequenceNumber.First);

    [Fact] void should_read_the_highest_stored_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
    [Fact] void should_preserve_the_appended_generation() => _read.Context.AppendedGeneration.ShouldEqual(EventTypeGeneration.First);
}
