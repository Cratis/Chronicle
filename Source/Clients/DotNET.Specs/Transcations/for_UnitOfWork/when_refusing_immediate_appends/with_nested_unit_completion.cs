// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_nested_unit_completion : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    Dictionary<string, Exception> _errors;
    bool _completedByNestedCaller;

    async Task Because()
    {
        using var owner = Refuse();
        ClearCalls();
        _errors = new()
        {
            ["commit"] = await Catch.Exception(_unit.Commit),
            ["rollback"] = await Catch.Exception(_unit.Rollback),
            ["dispose"] = Catch.Exception(_unit.Dispose),
            ["second claim"] = Catch.Exception(() => _unit.ClaimDecisionReadCommitOwnership())
        };
        _completedByNestedCaller = _unit.IsCompleted;
        await _sequence.Transactional.Append("source", "changed");
        await owner.Commit();
    }

    [Fact] void should_refuse_every_nested_completion_path() => _errors.Values.ShouldEachConformTo(error => error is ImmediateAppendRefusalRequiresOwner);
    [Fact] void should_cover_direct_completion_and_claiming() => _errors.Count.ShouldEqual(4);
    [Fact] void should_keep_the_unit_open_for_its_owner() => _completedByNestedCaller.ShouldBeFalse();
    [Fact] void should_only_send_the_final_owner_batch() => _sequences.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_commit_the_owner_event() => _request.Events.Count().ShouldEqual(1);
}
