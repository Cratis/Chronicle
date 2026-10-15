// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_an_event;

public class and_appended_generation_is_recorded : given.a_storage_for_appended_generation
{
    AppendedEvent _read;

    async Task Because()
    {
        var entry = EventAt(EventSequenceNumber.First, 1);
        (await _storage.Append(entry.SequenceNumber, entry.EventSourceType, entry.EventSourceId, entry.EventStreamType, entry.EventStreamId, entry.EventType, entry.CorrelationId, entry.Causation, entry.CausedByChain, entry.Tags, entry.Occurred, entry.GenerationalContent, entry.ContentHashes)).IsSuccess.ShouldBeTrue();
        _read = await _storage.GetEventAt(entry.SequenceNumber);
    }

    [Fact] void should_record_the_original_generation() => _storage.GetAppendedGeneration(EventSequenceNumber.First).ShouldEqual((uint?)1);
    [Fact] void should_read_the_highest_stored_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
}
