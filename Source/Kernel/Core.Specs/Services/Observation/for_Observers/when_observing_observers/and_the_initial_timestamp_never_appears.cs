// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_the_initial_timestamp_never_appears : given.an_observed_state_stream
{
    void Establish() => _observerStateStorage.GetRunningStatesChangedAt().Returns(new Dictionary<ObserverId, DateTimeOffset>());

    async Task Because()
    {
        for (ulong handled = 1; handled <= 7; handled++)
        {
            await Emit(("first", ObserverRunningState.Active, handled));
        }
        await EmittedCount(7);
    }

    [Fact] void should_stop_querying_after_the_follow_up_bound() => TimestampQueries.ShouldEqual(1 + RunningStatesChangedAtCache.FollowUpRefreshes);
}
