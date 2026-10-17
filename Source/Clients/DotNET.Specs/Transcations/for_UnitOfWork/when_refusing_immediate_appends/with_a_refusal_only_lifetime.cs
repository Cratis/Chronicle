// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_a_refusal_only_lifetime : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    bool _completedAfterDisposal;
    Exception _nextFrameError;
    Exception _appendError;

    async Task Because()
    {
        var completionOwner = _unit.ClaimDecisionReadCommitOwnership();
        using (var refusal = Refuse())
        {
            await _sequence.Transactional.Append("source", "changed");
        }
        _completedAfterDisposal = _unit.IsCompleted;
        using (var next = _unit.RefuseImmediateAppends())
        {
            _nextFrameError = await Catch.Exception(next.Commit);
            _appendError = await Catch.Exception(() => _sequence.Append("source", "escaped"));
        }
        ClearCalls();
        await _unit.CommitAsOwner(completionOwner);
    }

    [Fact] void should_leave_the_unit_open_on_disposal() => _completedAfterDisposal.ShouldBeFalse();
    [Fact] void should_keep_the_existing_owner_on_reacquisition() => _nextFrameError.ShouldBeOfExactType<ImmediateAppendRefusalRequiresOwner>();
    [Fact] void should_start_a_fresh_refusal_lifetime() => _appendError.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_preserve_events_for_the_existing_owner() => _request.Events.Count().ShouldEqual(1);
}
