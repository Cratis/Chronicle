// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

/// <summary>
/// A tail read that matches no event has no sequence number to report, and the only value the type has for that
/// is <see cref="EventSequenceNumber.Unavailable"/> - the same value that means "no sequence number was supplied".
/// That collapse is the bottom of the chain a concurrency scope resolves through: the optimistic strategy reads
/// the tail with the scope's own narrowing, so a narrowing that matches nothing, or an event source with nothing
/// on it at all, gives the scope no expected sequence number and the kernel then has nothing to validate against.
/// </summary>
public class when_reading_a_tail_that_matches_nothing : given.a_storage_with_appended_events
{
    static readonly EventSourceType _eventSourceTypeNothingWasAppendedWith = new("Customer");

    EventSequenceNumber _narrowedTail;
    EventSequenceNumber _unnarrowedTail;
    EventSequenceNumber _tailOfAnEventSourceWithNoEvents;

    async Task Because()
    {
        _narrowedTail = await _storage.GetTailSequenceNumber([], _firstEventSourceId, _eventSourceTypeNothingWasAppendedWith, EventStreamId.Default, EventStreamType.All);
        _unnarrowedTail = await _storage.GetTailSequenceNumber([], _firstEventSourceId, EventSourceType.Unspecified, EventStreamId.Default, EventStreamType.All);
        _tailOfAnEventSourceWithNoEvents = await _storage.GetTailSequenceNumber([], new EventSourceId("third"), EventSourceType.Unspecified, EventStreamId.Default, EventStreamType.All);
    }

    [Fact] void should_report_the_narrowed_tail_as_unavailable() => _narrowedTail.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_report_a_tail_for_the_same_event_source_without_the_narrowing() => _unnarrowedTail.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_report_the_tail_of_an_event_source_with_no_events_as_unavailable() => _tailOfAnEventSourceWithNoEvents.ShouldEqual(EventSequenceNumber.Unavailable);
}
