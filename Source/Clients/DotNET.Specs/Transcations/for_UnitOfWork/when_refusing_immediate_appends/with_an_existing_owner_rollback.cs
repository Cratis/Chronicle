// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_an_existing_owner_rollback : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    Exception _appendError;

    async Task Because()
    {
        var completionOwner = _unit.ClaimDecisionReadCommitOwnership();
        using var refusal = Refuse();
        await _sequence.Transactional.Append("source", "changed");
        ClearCalls();
        await _unit.RollbackAsOwner(completionOwner);
        _appendError = await Catch.Exception(() => _sequence.Append("source", "escaped"));
    }

    [Fact] void should_allow_the_existing_owner_to_roll_back() => _unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_discard_the_staged_events() => _unit.GetEvents().ShouldBeEmpty();
    [Fact] void should_not_reach_transport() => _sequences.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_refusal_until_disposal() => _appendError.ShouldBeOfExactType<ImmediateAppendRefused>();
}
