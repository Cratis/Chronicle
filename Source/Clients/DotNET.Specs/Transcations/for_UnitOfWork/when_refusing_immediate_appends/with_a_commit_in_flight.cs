// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_a_commit_in_flight : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    Exception _disposeError;
    Exception _rollbackError;

    async Task Because()
    {
        using var owner = Refuse();
        await _sequence.Transactional.Append("source", "changed");
        var release = new TaskCompletionSource<JsonObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(release.Task);
        ClearCalls();
        var commit = owner.Commit();
        try
        {
            _disposeError = Catch.Exception(owner.Dispose);
            _rollbackError = await Catch.Exception(owner.Rollback);
            _error = await Catch.Exception(() => _sequence.Append("source", "escaped"));
        }
        finally
        {
            release.TrySetResult(new JsonObject());
            await commit.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    [Fact] void should_not_lift_refusal_by_disposing_during_commit() => _disposeError.ShouldBeOfExactType<UnitOfWorkIsCompleting>();
    [Fact] void should_not_roll_back_an_in_flight_commit() => _rollbackError.ShouldBeOfExactType<UnitOfWorkIsCompleting>();
    [Fact] void should_keep_public_appends_refused() => _error.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_only_send_the_owner_batch() => _sequences.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_complete_the_owner_commit() => _unit.IsSuccess.ShouldBeTrue();
}
