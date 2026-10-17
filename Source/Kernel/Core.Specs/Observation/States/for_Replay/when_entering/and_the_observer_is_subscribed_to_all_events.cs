// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.States.for_Replay.when_entering;

public class and_the_observer_is_subscribed_to_all_events : given.a_replay_state
{
    ReplayObserverRequest _request;

    void Establish()
    {
        _observerDefinition = _observerDefinition with { Type = ObserverType.Projection };
        _storedState = _storedState with { SubscribesToAllEvents = true };
        _jobsManager
            .When(_ => _.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>()))
            .Do(callInfo => _request = callInfo.Arg<ReplayObserverRequest>());
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_the_replay_with_a_wildcard_scope() => _request.ReaderScope!.IsWildcard.ShouldBeTrue();
}
