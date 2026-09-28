// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class while_a_strict_owner_commit_is_pending : given.a_strict_unit_for_late_staging
{
    TaskCompletionSource<AppendManyResult> _append;
    Exception _ownerRollbackError;
    Exception _afterCommitRollbackError;
    Exception _publicRollbackError;
    Exception _disposeError;
    bool _openWhilePending;
    int _completionCount;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddDecisionRead(new DecisionRead<object>(
            "source", null, _eventStore.Name, _eventStore.Namespace, 1, [new EventType("created", 1)]));
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
        _ownerRollbackError = await Record.ExceptionAsync(() => _unitOfWork.RollbackAsOwner(owner));
        _publicRollbackError = await Record.ExceptionAsync(_unitOfWork.Rollback);
        _disposeError = Record.Exception(_unitOfWork.Dispose);
        _openWhilePending = !_unitOfWork.IsCompleted && _completionCount == 0 && _unitOfWork.GetEvents().Any();
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await commit;
        _afterCommitRollbackError = await Record.ExceptionAsync(() => _unitOfWork.RollbackAsOwner(owner));
    }

    [Fact] void should_refuse_owner_rollback_with_strict_lifecycle_error() => _ownerRollbackError.ShouldBeOfExactType<UnitOfWorkIsCompleting>();
    [Fact] void should_refuse_owner_rollback_after_commit() => _afterCommitRollbackError.ShouldBeOfExactType<UnitOfWorkIsAlreadyCommitted>();
    [Fact] void should_refuse_public_rollback_with_strict_lifecycle_error() => _publicRollbackError.ShouldBeOfExactType<UnitOfWorkIsCompleting>();
    [Fact] void should_not_interrupt_disposal_during_append() => _disposeError.ShouldBeNull();
    [Fact] void should_keep_the_unit_open_while_append_is_pending() => _openWhilePending.ShouldBeTrue();
    [Fact] void should_complete_only_once() => _completionCount.ShouldEqual(1);
    [Fact] void should_commit_the_append() => _unitOfWork.IsSuccess.ShouldBeTrue();
}
