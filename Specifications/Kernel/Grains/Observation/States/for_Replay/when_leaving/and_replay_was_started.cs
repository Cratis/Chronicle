// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Replay.when_leaving;

public class and_replay_was_started : given.a_replay_state
{
    ObserverDetails observer_details;

    async Task Establish()
    {
        stored_state = stored_state with
        {
            Type = ObserverType.Client
        };

        observer_service_client
            .Setup(_ => _.EndReplayFor(IsAny<ObserverDetails>()))
            .Callback((ObserverDetails observer) => observer_details = observer);

        stored_state = await state.OnEnter(stored_state);
    }

    async Task Because() => resulting_stored_state = await state.OnLeave(stored_state);

    [Fact] void should_end_replay_only_one() => observer_service_client.Verify(_ => _.BeginReplayFor(IsAny<ObserverDetails>()), Once);
    [Fact] void should_end_replay_for_correct_observer() => observer_details.ShouldEqual(new ObserverDetails(stored_state.ObserverId, observer_key, ObserverType.Client));
}
