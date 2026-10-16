// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_a_revised_event;

public class through_last_event_lookups : given.a_storage_with_two_stored_generations
{
    AppendedEvent _lastBefore;
    AppendedEvent _lastOfAny;

    async Task Because()
    {
        await ReviseAt(0, 1, "revised", "revision-hash");
        var before = await _storage.TryGetLastEventBefore(_eventType.Id, SourceId, 1);
        before.Match(option => option, exception => throw exception).TryGetValue(out _lastBefore!).ShouldBeTrue();
        (await _storage.TryGetLastInstanceOfAny(SourceId, [_eventType.Id])).TryGetValue(out _lastOfAny!).ShouldBeTrue();
    }

    [Fact] void should_read_the_revised_content_before_a_sequence_number() => ValueOf(_lastBefore).ShouldEqual("revised");
    [Fact] void should_read_the_revised_generation_before_a_sequence_number() => _lastBefore.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_read_the_revised_content_for_the_last_instance() => ValueOf(_lastOfAny).ShouldEqual("revised");
    [Fact] void should_read_the_revised_generation_for_the_last_instance() => _lastOfAny.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
}
