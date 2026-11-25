// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeSet;

public class when_creating_from_nothing : Specification
{
    EventTypeSet _fromNull;
    EventTypeSet _fromEmpty;

    void Because()
    {
        _fromNull = EventTypeSet.From(null);
        _fromEmpty = EventTypeSet.From([]);
    }

    [Fact] void should_be_the_empty_set_for_null() => ReferenceEquals(_fromNull, EventTypeSet.Empty).ShouldBeTrue();
    [Fact] void should_be_the_empty_set_for_an_empty_list() => ReferenceEquals(_fromEmpty, EventTypeSet.Empty).ShouldBeTrue();
    [Fact] void should_hold_nothing() => EventTypeSet.Empty.Count.ShouldEqual(0);
}
