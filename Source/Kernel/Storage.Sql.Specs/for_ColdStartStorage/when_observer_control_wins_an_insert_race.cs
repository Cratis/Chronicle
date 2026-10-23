// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_observer_control_wins_an_insert_race : given.a_cold_start_database
{
    KernelObserverState _saved;
    bool _accepted;

    async Task Because()
    {
        var storage = new ObserverStateStorage(Store, Namespace, _database);
        var control = new ObserverTypeSetControl(new(7), new("reactor-subscriber", ObserverFilters.None), new Dictionary<Concepts.Events.EventTypeId, ObserverTypeSetMember>(), []);
        await Race(
            async () => _accepted = await storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(42), TypeSetControl = control with { Revision = new(6) } }, Concepts.EventSequences.EventSequenceId.Log),
            () => storage.Save(new() { Identifier = "observer", TypeSetControl = control, Initialization = new(ObserverInitializationState.Initialized, ["reason"]) }, Concepts.EventSequences.EventSequenceId.Log));
        _saved = await storage.Get("observer");
    }

    [Fact] void should_preserve_the_newer_control() => _saved.TypeSetControl.Revision.Value.ShouldEqual(7);
    [Fact] void should_preserve_initialization() => _saved.Initialization.State.ShouldEqual(ObserverInitializationState.Initialized);
    [Fact] void should_refuse_the_losing_insert() => _accepted.ShouldBeFalse();
    [Fact] void should_preserve_the_winning_calls_progress() => _saved.NextEventSequenceNumber.Value.ShouldEqual(Concepts.Events.EventSequenceNumber.First.Value);
}
