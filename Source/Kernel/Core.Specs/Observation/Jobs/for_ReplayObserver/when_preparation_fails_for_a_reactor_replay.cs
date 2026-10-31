// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_preparation_fails_for_a_reactor_replay : given.a_replay_observer_job
{
    void Establish() => _request = _request with { ObserverType = ObserverType.Reactor };

    async Task Because()
    {
        await _job.Start(_request);
        await _job.FailPreparationForTesting();
        await _job.CompletionNotification;
    }

    [Fact] void should_finalize_the_replay() => _replayServiceClient.Received(1).EndReplayFor(Arg.Any<ObserverDetails>());
    [Fact] void should_reset_the_observer_cursor() => _observer.Received(1).Replayed(EventSequenceNumber.Unavailable);
    [Fact] async Task should_not_release_the_observer() => await _observer.DidNotReceive().ReleaseRefusedReplay();
}
