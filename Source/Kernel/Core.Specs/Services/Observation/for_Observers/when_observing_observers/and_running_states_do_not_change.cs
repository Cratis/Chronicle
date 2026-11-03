// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_running_states_do_not_change : given.an_observed_state_stream
{
    void Establish() => _observerStateStorage.GetRunningStatesChangedAt().Returns(Timestamps(_initialTimestamp));

    async Task Because()
    {
        for (ulong handled = 1; handled <= 4; handled++)
        {
            await Emit(("first", ObserverRunningState.Active, handled), ("second", ObserverRunningState.Active, handled));
        }
        await EmittedCount(4);
    }

    [Fact] void should_emit_every_snapshot() => _emitted.Count.ShouldEqual(4);
    [Fact] void should_query_timestamps_only_once() => _observerStateStorage.Received(1).GetRunningStatesChangedAt();
    [Fact] void should_carry_the_initial_timestamp() => _emitted.SelectMany(_ => _).ShouldEachConformTo(_ => (DateTimeOffset?)_.RunningStateChangedAt == _initialTimestamp);
}
