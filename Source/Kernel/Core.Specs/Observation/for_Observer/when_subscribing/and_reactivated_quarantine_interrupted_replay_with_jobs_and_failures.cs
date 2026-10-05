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

public class and_reactivated_quarantine_interrupted_replay_with_jobs_and_failures : given.a_reactivated_quarantined_observer
{
    JobId _catchUpJobId;
    FailedPartition _failedPartition;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true, FailedPartitionCount = 1 };
        var failures = new FailedPartitions();
        _failedPartition = failures.AddFailedPartition("failed-partition");
        _failedPartitionsStorage.State = failures;
        _catchUpJobId = JobId.New();
        var stoppedJob = new JobState
        {
            Id = _catchUpJobId,
            Status = JobStatus.Stopped,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [EventType.Unknown])
        };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(stoppedJob)));
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_resume_the_catch_up_job() => _jobsManager.Received(1).Resume(_catchUpJobId);
    [Fact] void should_recover_the_failed_partition() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == _failedPartition.Partition));
    [Fact] async Task should_return_to_replay() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
}
