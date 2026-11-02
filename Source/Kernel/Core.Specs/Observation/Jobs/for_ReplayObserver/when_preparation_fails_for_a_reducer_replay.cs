// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_preparation_fails_for_a_reducer_replay : given.a_replay_observer_job
{
    void Establish() => _request = _request with { ObserverType = ObserverType.Reducer };

    async Task Because()
    {
        await _job.Start(_request);
        await _job.FailPreparationForTesting();
        await _job.CompletionNotification;
    }

    [Fact] void should_abandon_the_replay() => _replayServiceClient.Received(1).AbandonReplayFor(Arg.Is<ObserverDetails>(_ => _.Type == ObserverType.Reducer));
    [Fact] void should_not_promote_the_rebuilt_read_model() => _replayServiceClient.DidNotReceive().EndReplayFor(Arg.Any<ObserverDetails>());
    [Fact] async Task should_report_that_the_replay_steps_could_not_be_prepared() => await _observer.Received(1).ReplayStepsCouldNotBePrepared();
    [Fact] async Task should_not_release_the_observer() => await _observer.DidNotReceive().ReleaseRefusedReplay();
    [Fact] void should_not_reset_the_observer_cursor() => _observer.DidNotReceive().Replayed(Arg.Any<EventSequenceNumber>());
}
