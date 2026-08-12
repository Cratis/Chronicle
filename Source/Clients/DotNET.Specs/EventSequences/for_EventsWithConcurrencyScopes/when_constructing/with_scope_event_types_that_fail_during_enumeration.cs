// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventsWithConcurrencyScopes.when_constructing;

public class with_scope_event_types_that_fail_during_enumeration : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _ = new EventsWithConcurrencyScopes(
        [new(EventSourceId.New(), new object())],
        [new(EventSourceId.New(), new ConcurrencyScope(42UL, EventTypes: FailingEventTypes()))]));

    [Fact] void should_surface_the_enumeration_failure_during_construction() => _error.ShouldBeOfExactType<InvalidOperationException>();

    static IEnumerable<EventType> FailingEventTypes()
    {
        yield return new EventType("event", 1);
        throw new InvalidOperationException("Event types could not be read");
    }
}
