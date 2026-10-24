// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_a_predecessor_is_stopped : given.an_observer_with_partition_replay_jobs
{
    readonly for_Observer.given.RecordingObserverLogger _recordedLogger = new();

    protected override ILogger<Observer> CreateLogger() => _recordedLogger;

    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        SetCatchupJobs(CatchupJob(_secondJob, JobStatus.Stopped));
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 48UL, [], _firstJob);
        await RunRetryTimer();
    }

    [Fact] void should_keep_the_fence() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_keep_the_unprepared_record() => _stateStorage.State.PartitionReplayCompletions!.Single().HandingOff.ShouldBeFalse();
    [Fact] void should_not_start_a_successor() => CheckDidNotStartCatchupJob();
    [Fact] void should_warn_about_the_stopped_job_on_every_reconcile() => _recordedLogger.Warnings.Count(message => message.Contains($"Waiting for stopped catch-up job '{_secondJob}'", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_keep_retrying_at_the_bounded_interval() => (RetryTimerIndex >= 0).ShouldBeTrue();
}
