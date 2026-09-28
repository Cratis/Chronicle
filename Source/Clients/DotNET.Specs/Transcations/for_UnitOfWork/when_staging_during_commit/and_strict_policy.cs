// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_staging_during_commit;

public class and_strict_policy : given.a_strict_unit_for_late_staging
{
    TaskCompletionSource<AppendManyResult> _append;

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
        var commit = _unitOfWork.Commit();
        AttemptLateStaging();
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await commit;
    }

    [Fact] void should_refuse_an_event() => _eventError.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
    [Fact] void should_refuse_a_batch() => _batchError.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
    [Fact] void should_not_enumerate_the_batch() => _batchWasEnumerated.ShouldBeFalse();
    [Fact] void should_leave_staged_events_unchanged() => _unitOfWork.GetEvents().ShouldContainOnly([_original]);
    [Fact] void should_only_append_the_original_event() => _eventSequence.Received(1).AppendMany(
        Arg.Is<IEnumerable<EventForEventSourceId>>(_ => _.Count() == 1 && _.Single().Event == _original),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
}
