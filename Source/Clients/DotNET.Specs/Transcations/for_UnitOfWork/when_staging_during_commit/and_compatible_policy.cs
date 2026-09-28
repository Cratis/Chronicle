// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_staging_during_commit;

public class and_compatible_policy : given.a_unit_for_late_staging
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

    [Fact] void should_accept_an_event() => _eventError.ShouldBeNull();
    [Fact] void should_accept_a_batch() => _batchError.ShouldBeNull();
    [Fact] void should_stage_both_attempts() => _unitOfWork.GetEvents().Count().ShouldEqual(3);
    [Fact] void should_log_both_attempts_at_error_level() => GetErrorLogs().Length.ShouldEqual(2);
    [Fact] void should_only_append_the_original_event() => _eventSequence.Received(1).AppendMany(
        Arg.Is<IEnumerable<EventForEventSourceId>>(_ => _.Count() == 1 && _.Single().Event == _original),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
}
