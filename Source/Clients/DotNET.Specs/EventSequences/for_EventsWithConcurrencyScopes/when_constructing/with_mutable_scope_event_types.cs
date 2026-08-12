// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventsWithConcurrencyScopes.when_constructing;

public class with_mutable_scope_event_types : Specification
{
    EventsWithConcurrencyScopes _result;
    EventType _eventType;
    List<EventType> _eventTypes;

    void Establish()
    {
        _eventType = new EventType("event", 1);
        _eventTypes = [_eventType];
        _result = new(
            [new(EventSourceId.New(), new object())],
            [new(EventSourceId.New(), new ConcurrencyScope(42UL, EventTypes: _eventTypes))]);
    }

    void Because() => _eventTypes.Clear();

    [Fact] void should_snapshot_the_nested_event_types() => _result.ConcurrencyScopes.Single().Value.EventTypes.ShouldContainOnly(_eventType);
    [Fact] void should_not_retain_the_mutable_collection() => ReferenceEquals(_result.ConcurrencyScopes.Single().Value.EventTypes, _eventTypes).ShouldBeFalse();
}
