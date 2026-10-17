// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_a_failed_owner_commit : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    readonly Exception _failure = new("transport failed");
    Exception _commitError;
    Exception _afterCleanupError;
    bool _failedUnit;

    async Task Because()
    {
        var owner = Refuse();
        try
        {
            await _sequence.Transactional.Append("source", "changed");
            _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>())
                .Returns(Task.FromException<CommandResult<Contracts.Sequences.AppendManyResponse>>(_failure));
            ClearCalls();
            _commitError = await Catch.Exception(owner.Commit);
            _failedUnit = _unit.IsCompleted && !_unit.IsSuccess;
            _error = await Catch.Exception(() => _sequence.Append("source", "escaped"));
        }
        finally
        {
            owner.Dispose();
        }
        _afterCleanupError = await Catch.Exception(() => _sequence.Append("source", "allowed"));
    }

    [Fact] void should_propagate_the_original_failure() => _commitError.ShouldEqual(_failure);
    [Fact] void should_not_report_a_failed_commit_as_successful() => _failedUnit.ShouldBeTrue();
    [Fact] void should_keep_refusal_until_cleanup() => _error.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_restore_the_previous_flow_after_cleanup() => _afterCleanupError.ShouldBeNull();
    [Fact] void should_send_only_the_failed_batch_and_the_post_cleanup_append() => _sequences.ReceivedCalls().Count().ShouldEqual(2);
}
