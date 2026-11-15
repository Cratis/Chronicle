// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// A catch-up job registers its partitions while preparing its steps, before its plan is durable. When the job then
/// ends Failed it owns nothing, so the watchdog releases the partitions it registered and routes again (#5532).
/// </summary>
public class and_a_catch_up_job_failed_to_start_after_registering_its_partitions : given.an_observer_with_client_owned_subscription
{
    static readonly Key _registeredPartition = new("registered-by-failed-job", ArrayIndexers.NoIndexers);

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));
        _stateStorage.State.CatchingUpPartitions.Add(_registeredPartition);

        IImmutableList<JobState> jobs = ImmutableList.Create(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Failed,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [event_type])
        });
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult(jobs));
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_release_the_partition_the_failed_job_registered() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_be_in_active_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
