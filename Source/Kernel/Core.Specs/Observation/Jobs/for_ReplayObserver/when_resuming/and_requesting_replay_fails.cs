// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_resuming;

public class and_requesting_replay_fails : given.a_replay_observer_job
{
    Exception? _error;

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _observer.GetState().Returns(ObserverState.Empty with { RunningState = ObserverRunningState.Active });
        _observer.Replay().Returns(Task.FromException<JobId>(new InvalidOperationException("The observer could not replay")));
    }

    async Task Because() => _error = await Catch.Exception(() => _job.ResumeForTesting());

    [Fact] void should_fail_to_resume() => _error.ShouldNotBeNull();
    [Fact] async Task should_not_switch_the_sinks_into_replay() => await _replayServiceClient.DidNotReceive().ResumeReplayFor(Arg.Any<ObserverDetails>());
}
