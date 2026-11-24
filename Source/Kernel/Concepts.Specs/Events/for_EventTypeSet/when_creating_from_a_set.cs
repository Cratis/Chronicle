// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeSet;

public class when_creating_from_a_set : Specification
{
    EventTypeSet _set;
    EventTypeSet _again;

    void Because()
    {
        _set = EventTypeSet.From([new EventType("again-type", 1)]);
        _again = EventTypeSet.From(_set);
    }

    [Fact] void should_return_the_same_set() => ReferenceEquals(_set, _again).ShouldBeTrue();
}
