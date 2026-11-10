// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_SweepHeldBackEvents;

public class when_more_is_held_back_than_is_kept : Specification
{
    static readonly EventType _eventType = new("event-type", EventTypeGeneration.First);
    SweepHeldBackEvents _heldBack;

    void Establish() => _heldBack = new(2);

    void Because()
    {
        _heldBack.Add("first", [EventAt(5), EventAt(7)]);
        _heldBack.Add("second", [EventAt(6)]);
        _heldBack.Add("first", [EventAt(8)]);
    }

    [Fact] void should_keep_only_the_most_recent_batches() => _heldBack.Count.ShouldEqual(2);
    [Fact] void should_not_cover_a_position_at_or_below_what_was_let_go() => _heldBack.Covers(7UL).ShouldBeFalse();
    [Fact] void should_cover_a_position_above_what_was_let_go() => _heldBack.Covers(8UL).ShouldBeTrue();
    [Fact] void should_hand_over_the_kept_events_from_a_position_in_delivery_order() =>
        _heldBack.From(6UL).Select(_ => _.Events.Single().Context.SequenceNumber.Value).SequenceEqual([6UL, 8UL]).ShouldBeTrue();

    static AppendedEvent EventAt(ulong sequenceNumber) => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_eventType, sequenceNumber);
}
