// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading;

public class without_appended_generation : given.a_storage_for_appended_generation
{
    EventEntry _stored;
    AppendedEvent _read;

    void Establish()
    {
        var entry = EventAt(0, 1);
        var legacy = EventEntryConverter.ToEventEntry(entry.SequenceNumber, entry.EventSourceType, entry.EventSourceId, entry.EventStreamType, entry.EventStreamId, entry.EventType, entry.CorrelationId, entry.Causation, entry.CausedByChain, entry.Tags, entry.Occurred, entry.GenerationalContent);
        legacy.AppendedGeneration = null;
        using var context = CreateContext();
        context.Events.Add(legacy);
        context.SaveChanges();
    }

    async Task Because()
    {
        _stored = Stored(0);
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_expose_unknown_appended_generation() => _read.Context.AppendedGeneration.ShouldBeNull();
    [Fact] void should_keep_legacy_revision_metadata_unknown() => _read.RevisedGeneration.ShouldBeNull();
    [Fact] void should_leave_the_original_generation_unknown() => _stored.AppendedGeneration.ShouldBeNull();
    [Fact] void should_still_read_the_highest_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_still_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
}
