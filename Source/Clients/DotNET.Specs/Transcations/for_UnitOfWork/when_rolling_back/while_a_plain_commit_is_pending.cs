// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class while_a_plain_commit_is_pending : given.a_unit_of_work
{
    TaskCompletionSource<AppendManyResult> _append;
    Exception _rollbackError;
    bool _rolledBackBeforeAppendFinished;
    bool _eventsCleared;
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
        var commit = _unitOfWork.Commit();
        _rollbackError = await Record.ExceptionAsync(_unitOfWork.Rollback);
        _rolledBackBeforeAppendFinished = _unitOfWork.IsCompleted;
        _eventsCleared = !_unitOfWork.GetEvents().Any();
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await commit;
    }

    [Fact] void should_allow_legacy_rollback() => _rollbackError.ShouldBeNull();
    [Fact] void should_complete_rollback_before_append_finishes() => _rolledBackBeforeAppendFinished.ShouldBeTrue();
    [Fact] void should_clear_staged_events() => _eventsCleared.ShouldBeTrue();
    [Fact] void should_preserve_legacy_completion_callbacks() => _completionCount.ShouldEqual(2);
    [Fact] void should_preserve_the_append_result() => _unitOfWork.IsSuccess.ShouldBeTrue();
}
