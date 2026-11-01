// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_observing_all;

public class and_only_the_running_state_timestamp_changes : given.an_observing_observer_state_storage
{
    static readonly DateTimeOffset _changedAt = new(2026, 5, 1, 10, 0, 0, TimeSpan.Zero);
    IReadOnlyDictionary<Concepts.Observation.ObserverId, DateTimeOffset> _timestamps;

    async Task Because()
    {
        await Subscribe();
        await _storage.SaveRunningStateChangedAt(_observerId, _changedAt, EventSequenceId.Log);
        await WaitForEmission();
        await WaitForCompletedScopes(6);
        _timestamps = await _storage.GetRunningStatesChangedAt();
    }

    [Fact] void should_emit_exactly_once_more() => _emissions.Count.ShouldEqual(2);
    [Fact] void should_emit_the_same_observer() => _emissions.Last().Single().Identifier.ShouldEqual(_observerId);
    [Fact] void should_emit_the_same_running_state() => _emissions.Last().Single().RunningState.ShouldEqual(_emissions.First().Single().RunningState);
    [Fact] void should_have_stored_the_timestamp() => _timestamps[_observerId].ShouldEqual(_changedAt);
}
