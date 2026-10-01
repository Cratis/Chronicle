// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_quarantine_begins_during_automatic_job_resumption : given.an_observer_automatically_reconciled_during_a_probe
{
    readonly TaskCompletionSource<IImmutableList<JobState>> _query = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly JobId _retryJobId = JobId.New();
    IImmutableList<JobState> _jobs;

    void Establish()
    {
        _jobs = ImmutableList.Create(new JobState
        {
            Id = _retryJobId,
            Status = JobStatus.Running,
            Request = new RetryFailedPartitionRequest(_observerKey, ObserverType.External, _partition, EventSequenceNumber.First, [EventType.Unknown])
        });
        _jobsManager.GetAllJobs().Returns(_ => _probeEntered.TrySetResult() ? _query.Task : Task.FromResult(_jobs));
    }

    async Task Because() => await QuarantineDuringProbe(ReconcileSubscription(), () => _query.SetResult(_jobs));

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_stop_the_retry_job() => _jobsManager.Received(1).Stop(_retryJobId);
    [Fact] void should_not_resume_jobs() => ShouldNotResumeJobs();
    [Fact] void should_not_start_replay() => ShouldNotStartReplay();
    [Fact] void should_not_start_catchup() => ShouldNotStartCatchup();
    [Fact] void should_not_resubscribe_to_the_queue() => ShouldNotSubscribeToQueue();
}
