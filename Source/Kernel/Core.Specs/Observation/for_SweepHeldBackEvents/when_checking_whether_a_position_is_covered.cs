// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_SweepHeldBackEvents;

public class when_checking_whether_a_position_is_covered : Specification
{
    static readonly EventType _eventType = new("event-type", EventTypeGeneration.First);
    SweepHeldBackEvents _heldBack;
    SweepHeldBackEvents _empty;

    void Establish()
    {
        _heldBack = new(10);
        _empty = new(10);
    }

    void Because() => _heldBack.Add("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_eventType, 5UL)]);

    [Fact] void should_cover_a_position_at_the_first_event_held_back() => _heldBack.Covers(5UL).ShouldBeTrue();
    [Fact] void should_cover_a_position_above_the_first_event_held_back() => _heldBack.Covers(9UL).ShouldBeTrue();
    [Fact] void should_not_cover_a_position_below_the_first_event_held_back() => _heldBack.Covers(4UL).ShouldBeFalse();
    [Fact] void should_not_cover_anything_when_nothing_was_held_back() => _empty.Covers(5UL).ShouldBeFalse();
}
