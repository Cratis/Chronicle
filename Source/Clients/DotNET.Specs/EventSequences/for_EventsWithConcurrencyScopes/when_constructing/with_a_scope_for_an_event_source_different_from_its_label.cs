// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventsWithConcurrencyScopes.when_constructing;

public class with_a_scope_for_an_event_source_different_from_its_label : Specification
{
    Exception _error;

    void Because()
    {
        var scopeLabel = EventSourceId.New();
        var narrowedEventSourceId = EventSourceId.New();
        _error = Catch.Exception(() => _ = new EventsWithConcurrencyScopes(
            [],
            [new(scopeLabel, new ConcurrencyScope(42UL, narrowedEventSourceId))]));
    }

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<ConcurrencyScopeEventSourceIdDoesNotMatchLabel>();
}
