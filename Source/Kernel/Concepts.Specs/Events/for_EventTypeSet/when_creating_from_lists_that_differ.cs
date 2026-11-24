// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeSet;

public class when_creating_from_lists_that_differ : Specification
{
    EventTypeSet _ordered;
    EventTypeSet _reversed;
    EventTypeSet _otherGeneration;

    void Because()
    {
        _ordered = EventTypeSet.From([new EventType("differ-a", 1), new EventType("differ-b", 1)]);
        _reversed = EventTypeSet.From([new EventType("differ-b", 1), new EventType("differ-a", 1)]);
        _otherGeneration = EventTypeSet.From([new EventType("differ-a", 2), new EventType("differ-b", 1)]);
    }

    [Fact] void should_not_share_when_the_order_differs() => ReferenceEquals(_ordered, _reversed).ShouldBeFalse();
    [Fact] void should_not_be_equal_when_the_order_differs() => _ordered.Equals(_reversed).ShouldBeFalse();
    [Fact] void should_not_share_when_a_generation_differs() => ReferenceEquals(_ordered, _otherGeneration).ShouldBeFalse();
}
