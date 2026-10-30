// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;
using Moq;
using Catch = Cratis.Specifications.Catch;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_webhook_partition_quarantine_and_starting_the_retry_job_fails : given.a_webhook_observer_with_an_overdue_deferral
{
    readonly given.RecordingObserverLogger _recordedLogger = new();
    ClearPartitionQuarantineResult _result;
    Exception? _error;

    protected override ILogger<Observer> CreateLogger() => _recordedLogger;

    void Establish()
    {
        _failedPartitionsState.Quarantine("partition");
        _startResult = () => Result<JobId, StartJobError>.Failed(StartJobError.NoJobStepsToStart);
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await _observer.ClearPartitionQuarantine("partition", true));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_report_cleared() => _result.Outcome.ShouldEqual(ClearPartitionQuarantineOutcome.Cleared);
    [Fact] void should_preserve_the_previous_retry_outcome() => _result.RetryOutcome.ShouldEqual(PartitionRecoveryOutcome.Started);
    [Fact] void should_clear_the_quarantine() => _failure.IsQuarantined.ShouldBeFalse();
    [Fact] void should_attempt_the_tracked_start() => _startCalls.ShouldEqual(1);
    [Fact] void should_log_the_start_failure() => _recordedLogger.Information.ShouldContain("Recovery job for partition partition did not start: StartFailed");

    [Fact]
    void should_preserve_the_retry_reminder() => _silo.ReminderRegistry.Mock.Verify(
        _ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failure.Id), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()),
        Times.Once);
}
