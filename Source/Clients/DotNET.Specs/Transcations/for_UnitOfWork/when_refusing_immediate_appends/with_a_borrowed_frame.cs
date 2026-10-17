// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_a_borrowed_frame : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    Exception _borrowCommitError;
    Exception _borrowRollbackError;
    bool _completedBeforeOwnerCommit;

    async Task Because()
    {
        using var owner = Refuse();
        var borrowed = _unit.RefuseImmediateAppends();
        await _sequence.Transactional.Append("source", "changed");
        _borrowCommitError = await Catch.Exception(borrowed.Commit);
        _borrowRollbackError = await Catch.Exception(borrowed.Rollback);
        borrowed.Dispose();
        borrowed.Dispose();
        _completedBeforeOwnerCommit = _unit.IsCompleted;
        _error = await Catch.Exception(() => _sequence.Append("source", "escaped"));
        ClearCalls();
        await owner.Commit();
    }

    [Fact] void should_not_let_the_borrower_commit() => _borrowCommitError.ShouldBeOfExactType<ImmediateAppendRefusalRequiresOwner>();
    [Fact] void should_not_let_the_borrower_roll_back() => _borrowRollbackError.ShouldBeOfExactType<ImmediateAppendRefusalRequiresOwner>();
    [Fact] void should_not_complete_the_unit_on_borrow_disposal() => _completedBeforeOwnerCommit.ShouldBeFalse();
    [Fact] void should_not_lift_refusal_on_borrow_disposal() => _error.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_let_the_original_owner_commit() => _request.Events.Count().ShouldEqual(1);
    [Fact] void should_only_send_the_owner_batch() => _sequences.ReceivedCalls().Count().ShouldEqual(1);
}
