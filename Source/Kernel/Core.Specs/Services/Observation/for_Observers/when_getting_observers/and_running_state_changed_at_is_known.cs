// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_getting_observers;

public class and_running_state_changed_at_is_known : given.all_dependencies
{
    readonly DateTimeOffset _timestamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    IEnumerable<ObserverInformation> _result;

    void Establish()
    {
        _observerDefinitionsStorage.GetAll().Returns(
        [
            Definition("known"),
            Definition("unknown")
        ]);
        _observerStateStorage.GetAll().Returns([]);
        _observerStateStorage.GetRunningStatesChangedAt().Returns(new Dictionary<Concepts.Observation.ObserverId, DateTimeOffset>
        {
            [new("known")] = _timestamp
        });
    }

    async Task Because() => _result = await _observers.GetObservers(new AllObserversRequest { EventStore = "some-event-store", Namespace = "some-namespace" });

    [Fact] void should_join_the_timestamp_by_observer() => ((DateTimeOffset?)_result.Single(_ => _.Id == "known").RunningStateChangedAt).ShouldEqual(_timestamp);
    [Fact] void should_leave_other_observers_unknown() => _result.Single(_ => _.Id == "unknown").RunningStateChangedAt.ShouldBeNull();

    static ObserverDefinition Definition(string id) => new(
        id,
        [],
        EventSequenceId.Log,
        Concepts.Observation.ObserverType.Reactor,
        Concepts.Observation.ObserverOwner.Client,
        false);
}
