// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.States.for_Replay.when_leaving;

public class and_replay_was_started : given.a_replay_state
{
    ObserverDetails observer_details;

    async Task Establish()
    {
        _storedState = _storedState with
        {
            Type = ObserverType.Reactor
        };

        _storedState = await _state.OnEnter(_storedState);
    }

    async Task Because() => _resultingStoredState = await _state.OnLeave(_storedState);

    [Fact] void should_not_do_anything() => _jobsManager.Received(0);
}
