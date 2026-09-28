// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_staging_after_completion;

public class and_compatible_unit_failed_to_commit : given.a_unit_for_late_staging
{
    Exception _commitError;

    void Establish() => _eventSequence.AppendMany(
        Arg.Any<IEnumerable<EventForEventSourceId>>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>>(),
        Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ => Task.FromException<AppendManyResult>(new InvalidOperationException("append failed")));

    async Task Because()
    {
        _commitError = await Catch.Exception(_unitOfWork.Commit);
        AttemptLateStaging();
    }

    [Fact] void should_propagate_commit_failure() => _commitError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_accept_an_event() => _eventError.ShouldBeNull();
    [Fact] void should_accept_a_batch() => _batchError.ShouldBeNull();
    [Fact] void should_stage_both_attempts() => _unitOfWork.GetEvents().Count().ShouldEqual(3);
    [Fact] void should_log_both_attempts_at_error_level() => GetErrorLogs().Length.ShouldEqual(2);
}
