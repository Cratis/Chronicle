// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;
using Catch = Cratis.Specifications.Catch;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_manually_starting_webhook_partition_recovery_and_starting_the_job_fails : given.a_webhook_observer_with_an_overdue_deferral
{
    readonly given.RecordingObserverLogger _recordedLogger = new();
    Exception? _error;
    Exception? _reminderError;

    protected override ILogger<Observer> CreateLogger() => _recordedLogger;

    void Establish() => _startResult = () => Result<JobId, StartJobError>.Failed(StartJobError.NoJobStepsToStart);

    async Task Because()
    {
        _error = await Catch.Exception(() => _observer.TryStartRecoverJobForFailedPartition("partition"));
        _reminderError = await Catch.Exception(Remind);
    }

    [Fact] void should_report_the_manual_start_failure() => _error.ShouldBeOfExactType<FailedPartitionRecoveryCouldNotStart>();
    [Fact] void should_identify_the_partition() => _error!.Message.ShouldContain("partition");
    [Fact] void should_log_the_start_failure() => _recordedLogger.Information.ShouldContain("Recovery job for partition partition did not start: StartFailed");
    [Fact] void should_not_throw_when_the_reminder_retries() => _reminderError.ShouldBeNull();
    [Fact] void should_forget_the_failed_tracked_start_so_the_reminder_can_retry() => _startCalls.ShouldEqual(2);
}
