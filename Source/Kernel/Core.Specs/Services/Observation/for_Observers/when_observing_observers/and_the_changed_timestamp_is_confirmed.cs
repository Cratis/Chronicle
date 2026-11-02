// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_the_changed_timestamp_is_confirmed : given.an_observed_state_stream
{
    void Establish() => _observerStateStorage.GetRunningStatesChangedAt().Returns(Timestamps(_initialTimestamp), Timestamps(_changedTimestamp));

    async Task Because()
    {
        await Emit(("first", ObserverRunningState.Active, 1));
        await Emit(("first", ObserverRunningState.Disconnected, 1));
        await Emit(("first", ObserverRunningState.Disconnected, 2));
        await Emit(("first", ObserverRunningState.Disconnected, 3));
        await EmittedCount(4);
    }

    [Fact] void should_not_query_again() => TimestampQueries.ShouldEqual(2);
}
