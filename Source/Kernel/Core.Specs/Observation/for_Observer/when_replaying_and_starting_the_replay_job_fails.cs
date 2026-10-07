// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_and_starting_the_replay_job_fails : given.an_observer_with_subscription
{
    static readonly JobId _earlierReplayJob = JobId.New();
    Exception? _error;
    JobId _replayJobAfterFailure = JobId.NotSet;

    async Task Establish()
    {
        _jobsManager
            .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(
                Task.FromResult(Result<JobId, StartJobError>.Success(_earlierReplayJob)),
                Task.FromException<Result<JobId, StartJobError>>(new InvalidOperationException("Starting the replay job failed")));

        await _observer.Replay();
        await _observer.Replayed(EventSequenceNumber.Unavailable);
    }

    async Task Because()
    {
        _error = await Cratis.Specifications.Catch.Exception(() => _observer.Replay());
        _replayJobAfterFailure = (await _observer.GetStates()).OfType<Replay>().First().LastStartedJobId;

        // Routing afterwards must not pick a replay that was never entered back up.
        await _observer.CaughtUp(JobId.New(), 1UL);
    }

    [Fact] void should_fail_the_replay_request() => _error.ShouldNotBeNull();
    [Fact] void should_not_keep_the_earlier_replay_job() => _replayJobAfterFailure.ShouldEqual(JobId.NotSet);
    [Fact] void should_be_observing_after_routing() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    async Task should_not_start_a_replay_when_routing() =>
        await _jobsManager.Received(2).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
