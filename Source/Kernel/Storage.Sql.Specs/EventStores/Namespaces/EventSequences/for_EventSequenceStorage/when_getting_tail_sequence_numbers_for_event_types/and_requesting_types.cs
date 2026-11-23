// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_getting_tail_sequence_numbers_for_event_types;

public class and_requesting_types : given.a_storage_with_tail_events
{
    static readonly EventType _typeAGenerationTwo = _typeA with { Generation = new EventTypeGeneration(2) };
    static readonly EventType _missing = new("never-appended", EventTypeGeneration.First);
    System.Collections.Immutable.IImmutableDictionary<EventType, EventSequenceNumber> _result;
    System.Collections.Immutable.IImmutableDictionary<EventType, EventSequenceNumber> _empty;

    async Task Establish() => await AppendTailEvents();

    async Task Because()
    {
        _result = await _storage.GetTailSequenceNumbersForEventTypes([_typeA, _typeAGenerationTwo, _typeB, _missing]);
        _empty = await _storage.GetTailSequenceNumbersForEventTypes([]);
    }

    [Fact] void should_return_an_entry_per_requested_type() => _result.Count.ShouldEqual(4);
    [Fact] void should_return_the_tail_of_the_first_type() => _result[_typeA].ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_return_the_tail_for_another_generation_of_the_same_type() => _result[_typeAGenerationTwo].ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_return_the_tail_of_the_second_type() => _result[_typeB].ShouldEqual((EventSequenceNumber)4UL);
    [Fact] void should_return_unavailable_for_a_type_without_events() => _result[_missing].ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_return_nothing_when_no_type_is_requested() => _empty.ShouldBeEmpty();
}
