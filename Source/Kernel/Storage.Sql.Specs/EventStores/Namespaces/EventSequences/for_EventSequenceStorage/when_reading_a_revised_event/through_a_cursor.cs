// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_a_revised_event;

public class through_a_cursor : given.a_storage_with_two_stored_generations
{
    AppendedEvent _fromSequenceNumber;
    AppendedEvent _fromRange;

    async Task Because()
    {
        await ReviseAt(0, 1, "revised", "revision-hash");
        using (var cursor = await _storage.GetFromSequenceNumber(0))
        {
            await cursor.MoveNext();
            _fromSequenceNumber = cursor.Current.Single();
        }

        using (var cursor = await _storage.GetRange(0, 0))
        {
            await cursor.MoveNext();
            _fromRange = cursor.Current.Single();
        }
    }

    [Fact] void should_read_the_revised_content_from_the_sequence_number() => ValueOf(_fromSequenceNumber).ShouldEqual("revised");
    [Fact] void should_read_the_revised_generation_from_the_sequence_number() => _fromSequenceNumber.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_read_the_revised_hash_from_the_sequence_number() => _fromSequenceNumber.Context.Hash.ShouldEqual(new EventHash("revision-hash"));
    [Fact] void should_read_the_revised_content_from_the_range() => ValueOf(_fromRange).ShouldEqual("revised");
    [Fact] void should_read_the_revised_generation_from_the_range() => _fromRange.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
}
