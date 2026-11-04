// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_a_transition_never_moves_the_timestamp : given.an_observed_state_stream
{
    void Establish() => _observerStateStorage.GetRunningStatesChangedAt().Returns(Timestamps(_initialTimestamp));

    async Task Because()
    {
        await Emit(("first", ObserverRunningState.Active, 1));
        await Emit(("first", ObserverRunningState.Disconnected, 1));
        for (ulong handled = 2; handled <= 7; handled++)
        {
            await Emit(("first", ObserverRunningState.Disconnected, handled));
        }
        await EmittedCount(8);
    }

    [Fact] void should_stop_querying_after_the_follow_up_bound() => TimestampQueries.ShouldEqual(2 + RunningStatesChangedAtCache.FollowUpRefreshes);
}
