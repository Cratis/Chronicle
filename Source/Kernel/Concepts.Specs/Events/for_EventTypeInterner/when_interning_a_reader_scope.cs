// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeInterner;

public class when_interning_a_reader_scope : Specification
{
    ObserverReaderScope _first;
    ObserverReaderScope _second;
    ObserverReaderScope _wildcard;

    void Because()
    {
        _first = EventTypeInterner.Intern(ObserverReaderScope.Explicit([new EventType("scoped-type", 1)]))!;
        _second = EventTypeInterner.Intern(ObserverReaderScope.Explicit([new EventType("scoped-type", 1)]))!;
        _wildcard = EventTypeInterner.Intern(ObserverReaderScope.Wildcard)!;
    }

    [Fact] void should_share_the_event_types() => ReferenceEquals(_first.EventTypes, _second.EventTypes).ShouldBeTrue();
    [Fact] void should_keep_it_explicit() => _first.IsWildcard.ShouldBeFalse();
    [Fact] void should_leave_a_wildcard_as_it_is() => _wildcard.IsWildcard.ShouldBeTrue();
    [Fact] void should_leave_nothing_as_nothing() => EventTypeInterner.Intern((ObserverReaderScope?)null).ShouldBeNull();
}
