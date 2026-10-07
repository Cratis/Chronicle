// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_resuming;

public class and_requesting_replay_fails : given.a_replay_observer_job
{
    Exception? _error;

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _observer.Replay().Returns(Task.FromException<JobId>(new InvalidOperationException("The observer could not replay")));
    }

    async Task Because() => _error = await Catch.Exception(() => _job.ResumeForTesting());

    [Fact] void should_still_resume() => _error.ShouldBeNull();
    [Fact] async Task should_resume_the_replay() => await _replayServiceClient.Received(1).ResumeReplayFor(Arg.Any<ObserverDetails>());
}
