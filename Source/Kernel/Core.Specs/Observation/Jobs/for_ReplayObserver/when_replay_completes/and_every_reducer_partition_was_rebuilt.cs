// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_every_reducer_partition_was_rebuilt : given.a_replay_observer_job
{
    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reducer };
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
        _stateStorage.State.Progress.TotalSteps = 2;
        _stateStorage.State.Progress.SuccessfulSteps = 2;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_promote_the_rebuilt_read_model() => _replayServiceClient.Received(1).EndReplayFor(Arg.Is<ObserverDetails>(_ => _.Type == ObserverType.Reducer));
    [Fact] void should_not_abandon_the_replay() => _replayServiceClient.DidNotReceive().AbandonReplayFor(Arg.Any<ObserverDetails>());
}
