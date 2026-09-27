// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class while_an_owner_commit_is_pending : given.a_unit_of_work
{
    TaskCompletionSource<AppendManyResult> _append;
    Exception _ownerRollbackError;
    Exception _publicRollbackError;
    Exception _disposeError;
    bool _stillOpen;
    bool _eventsStillStaged;
    bool _callbackNotCalled;
    int _completionCount;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddDecisionRead(new DecisionRead<object>(
            "source", null, _eventStore.Name, _eventStore.Namespace, 1, [new EventType("created", 1)]));
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
        _ownerRollbackError = await Record.ExceptionAsync(() => _unitOfWork.RollbackAsOwner(owner));
        IUnitOfWork publicUnit = _unitOfWork;
        _publicRollbackError = await Record.ExceptionAsync(publicUnit.Rollback);
        _disposeError = Record.Exception(_unitOfWork.Dispose);
        _stillOpen = !_unitOfWork.IsCompleted;
        _eventsStillStaged = _unitOfWork.GetEvents().Any();
        _callbackNotCalled = _completionCount == 0;
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await commit;
    }

    [Fact] void should_refuse_owner_rollback_during_commit() => _ownerRollbackError.ShouldBeOfExactType<DecisionReadAfterCompletion>();
    [Fact] void should_refuse_public_rollback_during_commit() => _publicRollbackError.ShouldBeOfExactType<DecisionReadAfterCompletion>();
    [Fact] void should_refuse_disposal_during_commit() => _disposeError.ShouldBeOfExactType<DecisionReadAfterCompletion>();
    [Fact] void should_remain_open_until_append_finishes() => _stillOpen.ShouldBeTrue();
    [Fact] void should_keep_events_until_append_finishes() => _eventsStillStaged.ShouldBeTrue();
    [Fact] void should_not_call_completion_before_append_finishes() => _callbackNotCalled.ShouldBeTrue();
    [Fact] void should_commit_when_append_finishes() => _unitOfWork.IsCompleted.ShouldBeTrue();
    [Fact] void should_report_the_actual_append_result() => _unitOfWork.IsSuccess.ShouldBeTrue();
    [Fact] void should_complete_only_once() => _completionCount.ShouldEqual(1);
    [Fact]
    void should_append_only_once() => _eventSequence.Received(1).AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
}
