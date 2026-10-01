// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_automatic_reconciliation_picks_up_wiped_storage : given.an_observer_with_reloadable_state
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = 100UL,
            LastHandledEventSequenceNumber = 99UL,
            InFlightPartitions = new HashSet<Key>(["stale-in-flight"]),
            CatchingUpPartitions = new HashSet<Key>(["stale-catchup"])
        };
        await _stateStorage.WriteStateAsync();
        await _reloadableStateStorage.ClearStateAsync();
        _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)100UL);
    }

    async Task Because() => await ReconcileSubscription();

    [Fact] void should_reset_the_live_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_persist_the_reset_cursor() => _reloadableStateStorage.PersistedState.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_reset_last_handled_progress() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_discard_stale_in_flight_markers() => _stateStorage.State.InFlightPartitions.ShouldBeEmpty();
    [Fact] void should_discard_stale_catchup_markers() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_be_active() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
