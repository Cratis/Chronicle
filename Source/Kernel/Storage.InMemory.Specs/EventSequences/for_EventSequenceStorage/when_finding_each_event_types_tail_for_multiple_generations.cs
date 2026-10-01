// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_finding_each_event_types_tail_for_multiple_generations : given.a_storage_with_events_of_multiple_generations
{
    IImmutableDictionary<EventType, EventSequenceNumber> _tails;

    async Task Because() => _tails = await _storage.GetTailSequenceNumbersForEventTypes([_tombstoneFilter, _otherEventType, _missingEventType]);

    [Fact] void should_find_the_latest_generation_by_type_id() => _tails[_tombstoneFilter].ShouldEqual((EventSequenceNumber)5);
    [Fact] void should_find_the_other_types_tail() => _tails[_otherEventType].ShouldEqual((EventSequenceNumber)6);
    [Fact] void should_report_unavailable_for_a_type_without_events() => _tails[_missingEventType].ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_preserve_the_requested_event_types_as_keys() => _tails.Keys.ShouldContainOnly(_tombstoneFilter, _otherEventType, _missingEventType);
}
