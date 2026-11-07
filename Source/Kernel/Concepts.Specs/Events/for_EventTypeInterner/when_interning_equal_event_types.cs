// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeInterner;

public class when_interning_equal_event_types : Specification
{
    EventType[] _first;
    EventType[] _second;

    void Because()
    {
        _first = EventTypeInterner.Intern([new EventType("first-type", 1), new EventType("second-type", 2)]);
        _second = EventTypeInterner.Intern([new EventType("first-type", 1), new EventType("second-type", 2)]);
    }

    [Fact] void should_share_the_array() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_keep_the_event_types_in_order() => _first.Select(_ => _.Id.Value).ShouldEqual(["first-type", "second-type"]);
}
