// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing_to_all_events;

public class and_client_disconnected_during_replay_with_stopped_jobs : given.a_replaying_observer_with_client_subscription
{
    JobState _catchUpJob;

    async Task Establish()
    {
        _catchUpJob = new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [event_type])
        };
        _jobsManager.GetAllJobs().Returns(_ => Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(_catchUpJob)));
        _jobsManager.When(_ => _.Stop(_catchUpJob.Id)).Do(_ => _catchUpJob.Status = JobStatus.Stopped);
        await DisconnectClient();
    }

    Task Because() => _observer.SubscribeToAllEvents<IClientOwnedObserverSubscriber>(ObserverType.Reactor, SiloAddress.Zero, _connectedClient);

    [Fact] void should_have_paused_the_catch_up_job_on_disconnect() => _catchUpJob.Status.ShouldEqual(JobStatus.Stopped);
    [Fact] void should_resume_the_catch_up_job() => _jobsManager.Received(1).Resume(_catchUpJob.Id);
    [Fact] async Task should_return_to_replay() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
    [Fact] void should_subscribe_to_all_events() => _stateStorage.State.SubscribesToAllEvents.ShouldBeTrue();
}
