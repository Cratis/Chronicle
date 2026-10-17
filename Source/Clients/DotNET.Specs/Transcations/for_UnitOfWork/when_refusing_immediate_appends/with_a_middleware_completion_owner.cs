// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_a_middleware_completion_owner : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    Exception _frameCommitError;
    Exception _frameRollbackError;
    Exception _nestedCommitError;
    Exception _appendError;

    async Task Because()
    {
        // UnitOfWorkMiddleware claims this capability before invoking application code.
        var completionOwner = _unit.ClaimDecisionReadCommitOwnership();
        using var refusal = Refuse();
        _unit.AddDecisionRead(new DecisionRead<object>("dependency", null, "store", "tenant", 7, [new EventType("changed", EventTypeGeneration.First)]));
        await _sequence.Transactional.Append("target", "changed");
        _frameCommitError = await Catch.Exception(refusal.Commit);
        _frameRollbackError = await Catch.Exception(refusal.Rollback);
        _nestedCommitError = await Catch.Exception(_unit.Commit);
        ClearCalls();
        await _unit.CommitAsOwner(completionOwner);
        _appendError = await Catch.Exception(() => _sequence.Append("source", "escaped"));
    }

    [Fact] void should_leave_commit_with_the_middleware_owner() => _frameCommitError.ShouldBeOfExactType<ImmediateAppendRefusalRequiresOwner>();
    [Fact] void should_leave_rollback_with_the_middleware_owner() => _frameRollbackError.ShouldBeOfExactType<ImmediateAppendRefusalRequiresOwner>();
    [Fact] void should_preserve_protected_completion_admission() => _nestedCommitError.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
    [Fact] void should_preserve_the_read_dependency() => _request.ConcurrencyScopes.Single(scope => scope.EventSourceId == "dependency").Scope.SequenceNumber.ShouldEqual(7UL);
    [Fact] void should_send_one_owner_batch() => _sequences.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_complete_successfully() => _unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_refusal_after_owner_completion() => _appendError.ShouldBeOfExactType<ImmediateAppendRefused>();
}
