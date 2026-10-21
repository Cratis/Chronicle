// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_a_protected_read_inside_a_non_enforcing_unit : given.a_protected_reader
{
    Exception _scopeError;
    Exception _commitError;

    async Task Because()
    {
        await _reader.GetFor<Model>(_unit, "source");
        _scopeError = Catch.Exception(() => _unit.AddEvent(EventSequenceId.Log, "source", new Created(), Causation.Unknown(), concurrencyScope: new ConcurrencyScope(5, "source")));
        _commitError = await Catch.Exception(_unit.Commit);
    }

    [Fact] void should_not_upgrade_the_outer_unit() => _unit.IsEnforcingReadProtection.ShouldBeFalse();
    [Fact] void should_enroll_the_protected_read_before_returning() => _scopeError.ShouldBeOfExactType<DecisionReadConflictsWithConcurrencyScope>();
    [Fact] void should_still_require_the_decision_owner() => _commitError.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
}
