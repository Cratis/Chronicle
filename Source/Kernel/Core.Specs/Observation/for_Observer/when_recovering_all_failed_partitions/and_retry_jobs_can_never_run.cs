// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_all_failed_partitions;

/// <summary>
/// Stopped retry jobs that were never prepared, or that belong to an old failure episode, can never resume and are
/// dropped. A stopped job of the current recovery is kept and resumed, and so is a job of another observer.
/// </summary>
public class and_retry_jobs_can_never_run : given.an_observer
{
    const string Partition = "event-source-a";

    JobState _neverPrepared;
    JobState _staleEpisode;
    JobState _current;
    JobState _otherObserver;

    void Establish()
    {
        var failedPartitions = new FailedPartitions();
        failedPartitions.AddFailedPartition(Partition);
        _failedPartitionsStorage.State = failedPartitions;
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };

        var otherObserverKey = _observerKey with { ObserverId = new ObserverId("some-other-observer") };
        _neverPrepared = given.stopped_retry_jobs.NeverPrepared(_observerKey, Partition);
        _staleEpisode = given.stopped_retry_jobs.Prepared(_observerKey, Partition, FailedPartitionId.New());
        _current = given.stopped_retry_jobs.Prepared(_observerKey, Partition);
        _otherObserver = given.stopped_retry_jobs.NeverPrepared(otherObserverKey, Partition);
        HasOutstandingJobs(_neverPrepared, _staleEpisode, _current, _otherObserver);
        _jobsManager.Resume(_current.Id).Returns(true);
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_drop_the_job_that_was_never_prepared() => await _jobsManager.Received().Delete(_neverPrepared.Id);
    [Fact] async Task should_drop_the_job_of_the_old_episode() => await _jobsManager.Received().Delete(_staleEpisode.Id);
    [Fact] async Task should_keep_the_job_of_the_current_recovery() => await _jobsManager.DidNotReceive().Delete(_current.Id);
    [Fact] async Task should_not_touch_the_job_of_another_observer() => await _jobsManager.DidNotReceive().Delete(_otherObserver.Id);
    [Fact] async Task should_resume_the_job_of_the_current_recovery() => await _jobsManager.Received().Resume(_current.Id);

    [Fact]
    void should_not_start_a_new_job() => _jobsManager
        .DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
