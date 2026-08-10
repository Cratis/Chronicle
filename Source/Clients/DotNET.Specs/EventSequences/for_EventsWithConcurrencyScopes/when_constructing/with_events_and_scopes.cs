// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventsWithConcurrencyScopes.when_constructing;

public class with_events_and_scopes : Specification
{
    EventsWithConcurrencyScopes _result;
    EventForEventSourceId _firstEvent;
    EventForEventSourceId _secondEvent;
    EventSourceId _firstScopeKey;
    EventSourceId _secondScopeKey;
    ConcurrencyScope _firstScope;
    ConcurrencyScope _secondScope;
    List<EventForEventSourceId> _events;
    List<KeyValuePair<EventSourceId, ConcurrencyScope>> _scopes;

    void Establish()
    {
        _firstEvent = new(EventSourceId.New(), new object());
        _secondEvent = new(EventSourceId.New(), new object());
        _firstScopeKey = EventSourceId.New();
        _secondScopeKey = EventSourceId.New();
        _firstScope = new(new(42));
        _secondScope = ConcurrencyScope.NotSet;
        _events = [_firstEvent, _secondEvent];
        _scopes =
        [
            new(_firstScopeKey, _firstScope),
            new(_secondScopeKey, _secondScope)
        ];
    }

    void Because()
    {
        _result = new(_events, _scopes);
        _events.Reverse();
        _scopes.Clear();
    }

    [Fact] void should_materialize_the_events() => _result.Events.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_first_event() => _result.Events[0].ShouldEqual(_firstEvent);
    [Fact] void should_preserve_the_second_event() => _result.Events[1].ShouldEqual(_secondEvent);
    [Fact] void should_copy_every_scope() => _result.ConcurrencyScopes.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_first_scope_exactly() => _result.ConcurrencyScopes[_firstScopeKey].ShouldEqual(_firstScope);
    [Fact] void should_preserve_the_not_set_scope_exactly() => _result.ConcurrencyScopes[_secondScopeKey].ShouldEqual(_secondScope);
}
