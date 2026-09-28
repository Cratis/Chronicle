// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_disposing;

public class while_a_plain_commit_is_pending : given.a_unit_of_work
{
    TaskCompletionSource<AppendManyResult> _append;
    Exception _disposeError;
    bool _rolledBackBeforeAppendFinished;
    bool _eventsCleared;
    int _completionCount;

    void Establish()
    {
        // A claim without an enrolled read does not protect a legacy unit.
        _unitOfWork.ClaimDecisionReadCommitOwnership();
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
        _disposeError = Record.Exception(_unitOfWork.Dispose);
        _rolledBackBeforeAppendFinished = _unitOfWork.IsCompleted;
        _eventsCleared = !_unitOfWork.GetEvents().Any();
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await commit;
    }

    [Fact] void should_allow_legacy_disposal() => _disposeError.ShouldBeNull();
    [Fact] void should_not_complete_before_append_finishes() => _rolledBackBeforeAppendFinished.ShouldBeFalse();
    [Fact] void should_keep_staged_events() => _eventsCleared.ShouldBeFalse();
    [Fact] void should_complete_once_after_append_finishes() => _completionCount.ShouldEqual(1);
    [Fact] void should_preserve_the_append_result() => _unitOfWork.IsSuccess.ShouldBeTrue();
}
