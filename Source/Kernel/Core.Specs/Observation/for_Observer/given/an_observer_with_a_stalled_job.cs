// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_a_stalled_job : when_watchdog_runs.given.an_observer_with_client_owned_subscription
{
    protected JobState _job;
    protected DateTimeOffset _now;

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));
        _now = DateTimeOffset.UtcNow;
        _observer.StalledJobsTimeProvider = new StalledJobClock(() => _now);
        _stateStorage.State = _stateStorage.State with { IsReplaying = true, RunningState = ObserverRunningState.Replaying };
        _job = new JobState
        {
            Id = JobId.New(),
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [event_type]),
            Status = JobStatus.Running,
            StatusChanges = [new JobStatusChanged { Status = JobStatus.Running, Occurred = _now }],
            Progress = new() { TotalSteps = 10 }
        };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(_job)));
        ResumeAnswers(ResumeJobSuccess.Success);
        _jobsManager.ClearReceivedCalls();
    }

    protected void AdvanceClock() => _now += JobLiveness.ResumeStalledAfter + TimeSpan.FromMinutes(1);

    protected void ResumeAnswers(Result<ResumeJobSuccess, ResumeJobError> result) =>
        _jobsManager.ResumeIfRunning(_job.Id).Returns(Task.FromResult(result));

    sealed class StalledJobClock(Func<DateTimeOffset> now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now();
    }
}
