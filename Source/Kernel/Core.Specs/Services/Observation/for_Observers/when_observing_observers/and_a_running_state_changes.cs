// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_a_running_state_changes : given.an_observed_state_stream
{
    void Establish() => _observerStateStorage.GetRunningStatesChangedAt().Returns(Timestamps(_initialTimestamp), Timestamps(_changedTimestamp));

    async Task Because()
    {
        await Emit(("first", ObserverRunningState.Active, 1));
        await Emit(("first", ObserverRunningState.Disconnected, 1));
        await EmittedCount(2);
    }

    [Fact] void should_refresh_the_timestamps() => TimestampQueries.ShouldEqual(2);
    [Fact] void should_carry_the_new_timestamp() => ((DateTimeOffset?)_emitted[^1].Single(_ => _.Id == "first").RunningStateChangedAt).ShouldEqual(_changedTimestamp);
}
