// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_the_state_lands_before_its_timestamp : given.an_observed_state_stream
{
    void Establish() => _observerStateStorage.GetRunningStatesChangedAt().Returns(Timestamps(_initialTimestamp), Timestamps(_initialTimestamp), Timestamps(_changedTimestamp));

    async Task Because()
    {
        await Emit(("first", ObserverRunningState.Active, 1));
        await Emit(("first", ObserverRunningState.Disconnected, 1));
        await Emit(("first", ObserverRunningState.Disconnected, 1));
        await EmittedCount(3);
    }

    [Fact] void should_query_again_for_the_unconfirmed_transition() => TimestampQueries.ShouldEqual(3);
    [Fact] void should_end_with_the_new_timestamp() => ((DateTimeOffset?)_emitted[^1].Single(_ => _.Id == "first").RunningStateChangedAt).ShouldEqual(_changedTimestamp);
}
