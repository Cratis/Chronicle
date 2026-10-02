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

public class and_client_disconnected_during_replay_with_stopped_jobs : given.a_replaying_observer_with_client_subscription
{
    JobState[] _jobs;

    async Task Establish()
    {
        _jobs =
        [
            new() { Id = JobId.New(), Status = JobStatus.Running, Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [event_type]) },
            new() { Id = JobId.New(), Status = JobStatus.Running, Request = new RetryFailedPartitionRequest(_observerKey, ObserverType.Reactor, "failed-partition", EventSequenceNumber.First, [event_type]) },
            new() { Id = JobId.New(), Status = JobStatus.Running, Request = new ReplayObserverPartitionRequest(_observerKey, ObserverType.Reactor, "replaying-partition", EventSequenceNumber.First, EventSequenceNumber.Max, [event_type]) }
        ];
        _jobsManager.GetAllJobs().Returns(_ => Task.FromResult<IImmutableList<JobState>>(_jobs.ToImmutableList()));
        _jobsManager.When(_ => _.Stop(Arg.Any<JobId>()))
            .Do(call => _jobs.Single(job => job.Id == call.Arg<JobId>()).Status = JobStatus.Stopped);
        await DisconnectClient();
    }

    Task Because() => _observer.Subscribe<IClientOwnedObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _connectedClient);

    [Fact] void should_have_paused_the_non_replay_jobs_on_disconnect() => _jobs.All(job => job.Status == JobStatus.Stopped).ShouldBeTrue();
    [Fact] void should_resume_the_catch_up_job() => _jobsManager.Received(1).Resume(_jobs[0].Id);
    [Fact] void should_resume_the_failed_partition_retry_job() => _jobsManager.Received(1).Resume(_jobs[1].Id);
    [Fact] void should_resume_the_partition_replay_job() => _jobsManager.Received(1).Resume(_jobs[2].Id);
    [Fact] async Task should_return_to_replay() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
    [Fact] void should_not_start_a_second_replay_job() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
