// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class while_a_plain_owner_commit_is_pending : given.a_unit_of_work
{
    TaskCompletionSource<AppendManyResult> _append;
    Exception _rollbackError;
    bool _openWhilePending;
    int _completionCount;

    void Establish()
    {
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
        _unitOfWork.OnCompleted(_ => _completionCount++);
        _append = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ => _append.Task);
    }

    async Task Because()
    {
        var owner = _unitOfWork.ClaimDecisionReadCommitOwnership();
        var commit = _unitOfWork.CommitAsOwner(owner);
        _rollbackError = await Record.ExceptionAsync(() => _unitOfWork.RollbackAsOwner(owner));
        _openWhilePending = !_unitOfWork.IsCompleted && _unitOfWork.GetEvents().Any() && _completionCount == 0;
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await commit;
    }

    [Fact] void should_refuse_owner_rollback_during_plain_commit() => _rollbackError.ShouldBeOfExactType<UnitOfWorkIsCompleting>();
    [Fact] void should_leave_the_pending_append_alone() => _openWhilePending.ShouldBeTrue();
    [Fact] void should_complete_only_once() => _completionCount.ShouldEqual(1);
    [Fact] void should_preserve_the_append_result() => _unitOfWork.IsSuccess.ShouldBeTrue();
}
