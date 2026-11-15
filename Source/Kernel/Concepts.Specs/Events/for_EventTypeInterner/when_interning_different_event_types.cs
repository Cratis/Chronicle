// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeInterner;

public class when_interning_different_event_types : Specification
{
    EventType[] _first;
    EventType[] _other;
    EventType[] _otherGeneration;

    void Because()
    {
        _first = EventTypeInterner.Intern([new EventType("some-type", 1)]);
        _other = EventTypeInterner.Intern([new EventType("some-other-type", 1)]);
        _otherGeneration = EventTypeInterner.Intern([new EventType("some-type", 2)]);
    }

    [Fact] void should_not_share_arrays_of_different_types() => ReferenceEquals(_first, _other).ShouldBeFalse();
    [Fact] void should_not_share_arrays_of_different_generations() => ReferenceEquals(_first, _otherGeneration).ShouldBeFalse();
    [Fact] void should_keep_the_generation() => _otherGeneration[0].Generation.Value.ShouldEqual(2u);
}
