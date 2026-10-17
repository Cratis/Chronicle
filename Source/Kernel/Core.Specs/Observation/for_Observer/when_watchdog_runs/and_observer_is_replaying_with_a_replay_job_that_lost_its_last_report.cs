// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// A replay job recorded as running whose every step concluded long ago will never finish by itself, so it cannot
/// be what the replaying observer is waiting on.
/// </summary>
public class and_observer_is_replaying_with_a_replay_job_that_lost_its_last_report : given.an_observer_with_client_owned_subscription
{
    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };

        var lostJob = new JobState
        {
            Id = JobId.New(),
            Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, []),
            Status = JobStatus.Running,
            Progress = new() { TotalSteps = 3, SuccessfulSteps = 3 },
            StatusChanges = [new JobStatusChanged { Status = JobStatus.Running, Occurred = DateTimeOffset.UtcNow - TimeSpan.FromHours(1) }]
        };

        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty.Add(lostJob)));
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_treat_the_replay_as_having_no_live_job_and_start_one() => _jobsManager.Received(1)
        .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
