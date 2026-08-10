// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventsWithConcurrencyScopes.when_constructing;

public class with_duplicate_scope_keys : Specification
{
    Exception _error;
    EventSourceId _scopeKey;

    void Establish() => _scopeKey = EventSourceId.New();

    void Because() => _error = Catch.Exception(() => _ = new EventsWithConcurrencyScopes(
        [],
        [
            new(_scopeKey, ConcurrencyScope.None),
            new(_scopeKey, ConcurrencyScope.NotSet)
        ]));

    [Fact] void should_fail_for_the_duplicate_key() => _error.ShouldBeOfExactType<DuplicateConcurrencyScopeForEventSourceId>();
}
