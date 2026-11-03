// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.States.for_Replay.when_entering_with_ownership_claims;

public class and_a_stopped_replay_job_holds_a_superseded_claim : given.a_replay_state_with_ownership_claims
{
    JobState _stoppedJob;

    void Establish()
    {
        var request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, []) { OwnershipEpoch = 3 };
        _stoppedJob = new()
        {
            Id = JobId.New(),
            Request = request,
            StatusChanges = [new() { Status = JobStatus.Running, Occurred = DateTimeOffset.UtcNow }, new() { Status = JobStatus.Stopped, Occurred = DateTimeOffset.UtcNow }],
            Status = JobStatus.Stopped
        };
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(new[] { _stoppedJob }.ToImmutableList());
        _claims.IsObsolete(request).Returns(true);
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_resume_the_job_admission_would_refuse() => _jobsManager.DidNotReceive().Resume(_stoppedJob.Id);
    [Fact] void should_start_a_job_under_a_new_claim() => _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
