// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_and_already_replaying : given.an_observer_with_subscription
{
    static readonly JobId _replayJob = JobId.New();
    JobId _result = JobId.NotSet;

    async Task Establish()
    {
        _jobsManager
            .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_replayJob)));
        await _observer.Replay();
    }

    async Task Because() => _result = await _observer.Replay();

    [Fact] void should_return_the_running_replay_job() => _result.ShouldEqual(_replayJob);
    [Fact] void should_still_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);

    [Fact]
    async Task should_not_start_another_replay() =>
        await _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
