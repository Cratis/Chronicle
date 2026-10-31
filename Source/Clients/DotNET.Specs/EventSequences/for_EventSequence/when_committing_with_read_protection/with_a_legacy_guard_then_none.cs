// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_legacy_guard_then_none : given.a_trusted_commit
{
    void Establish()
    {
        _unit.AddEvent(EventSequenceId.Log, "source", "first", Causation.Unknown(), concurrencyScope: new ConcurrencyScope(42, "source", "stream"));
        _unit.AddEvent(EventSequenceId.Log, "source", "second", Causation.Unknown(), concurrencyScope: ConcurrencyScope.None);
    }

    async Task Because() => _error = await Catch.Exception(Commit);

    [Fact] void should_refuse_weakening_the_guard() => _error.ShouldBeOfExactType<DuplicateConcurrencyScopeForEventSourceId>();
    [Fact] void should_not_send_the_request() => _request.ShouldBeNull();
}
