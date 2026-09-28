// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class and_compatible_commit_is_pending : given.a_unit_for_late_staging
{
    TaskCompletionSource<AppendManyResult> _append;
    Exception _rollbackError;
    Task _commit;
    bool _eventsWereDiscarded;

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
        _rollbackError = await Catch.Exception(_unitOfWork.Rollback);
        _eventsWereDiscarded = !_unitOfWork.GetEvents().Any();
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await _commit;
    }

    [Fact] void should_preserve_legacy_rollback() => _rollbackError.ShouldBeNull();
    [Fact] void should_discard_staged_events() => _eventsWereDiscarded.ShouldBeTrue();
    [Fact] void should_finish_the_in_flight_commit() => _commit.IsCompletedSuccessfully.ShouldBeTrue();
}
