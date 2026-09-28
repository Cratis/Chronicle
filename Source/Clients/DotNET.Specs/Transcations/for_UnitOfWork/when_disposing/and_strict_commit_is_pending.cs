// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_disposing;

public class and_strict_commit_is_pending : given.a_strict_unit_for_late_staging
{
    TaskCompletionSource<AppendManyResult> _append;
    Exception _disposeError;
    Task _commit;
    bool _stagedEventsRetained;

    void Establish()
    {
        _append = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ => _append.Task);
    }

    async Task Because()
    {
        _commit = _unitOfWork.Commit();
        _disposeError = Record.Exception(_unitOfWork.Dispose);
        _stagedEventsRetained = _unitOfWork.GetEvents().SingleOrDefault() == _original;
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await _commit;
    }

    [Fact] void should_not_throw() => _disposeError.ShouldBeNull();
    [Fact] void should_not_discard_staged_events() => _stagedEventsRetained.ShouldBeTrue();
    [Fact] void should_finish_the_in_flight_commit() => _commit.IsCompletedSuccessfully.ShouldBeTrue();
}
