// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_and_the_observer_cannot_replay_from_its_state : given.an_observer_with_subscription
{
    static readonly JobId _earlierReplayJob = JobId.New();
    JobId _result = JobId.NotSet;

    async Task Establish()
    {
        _jobsManager
            .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_earlierReplayJob)));
        await _observer.Replay();
        await _observer.Unsubscribe();
    }

    async Task Because() => _result = await _observer.Replay();

    [Fact] void should_not_return_the_earlier_replay_job() => _result.ShouldEqual(JobId.NotSet);
    [Fact] void should_stay_disconnected() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);

    [Fact]
    async Task should_not_start_another_replay() =>
        await _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
