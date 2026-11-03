// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_the_initial_state_lands_before_its_timestamp : given.an_observed_state_stream
{
    void Establish() => _observerStateStorage.GetRunningStatesChangedAt().Returns(new Dictionary<ObserverId, DateTimeOffset>(), Timestamps(_changedTimestamp));

    async Task Because()
    {
        await Emit(("first", ObserverRunningState.Active, 1));
        await EmittedCount(1);
        await Emit(("first", ObserverRunningState.Active, 1));
        await EmittedCount(2);
    }

    [Fact] void should_refresh_for_the_unconfirmed_initial_state() => TimestampQueries.ShouldEqual(2);
    [Fact] void should_end_with_the_persisted_timestamp() => ((DateTimeOffset?)_emitted[^1].Single(_ => _.Id == "first").RunningStateChangedAt).ShouldEqual(_changedTimestamp);
}
