// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_initialization : given.an_observer_state_storage
{
    KernelObserverState _legacy;
    KernelObserverState _saved;
    KernelObserverState _renamed;
    ObserverInitializationState[] _states;

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "legacy" });
        _legacy = await _storage.Get("legacy");
        var restored = new List<ObserverInitializationState>();
        foreach (var state in Enum.GetValues<ObserverInitializationState>())
        {
            await _storage.Save(new() { Identifier = "observer", Initialization = new(state, ["unfinished job", "unsettled handover"]), RunningState = ObserverRunningState.Initializing });
            restored.Add((await _storage.Get("observer")).Initialization.State);
        }

        _states = restored.ToArray();
        await _storage.Save(new() { Identifier = "observer", RunningState = ObserverRunningState.ChangingDefinition });
        _saved = await _storage.Get("observer");
        await _storage.Rename("observer", "renamed");
        _renamed = await _storage.Get("renamed");
    }

    [Fact] void should_leave_legacy_initialization_absent() => _legacy.Initialization.ShouldBeNull();
    [Fact] void should_keep_all_four_initialization_states() => _states.ShouldEqual(Enum.GetValues<ObserverInitializationState>());
    [Fact] void should_keep_initialization_on_a_legacy_save() => _saved.Initialization.State.ShouldEqual(ObserverInitializationState.Initialized);
    [Fact] void should_keep_every_refusal_reason() => _saved.Initialization.RefusalReasons.ShouldEqual("unfinished job", "unsettled handover");
    [Fact] void should_keep_initialization_on_rename() => _renamed.Initialization.State.ShouldEqual(ObserverInitializationState.Initialized);
    [Fact] void should_keep_reasons_on_rename() => _renamed.Initialization.RefusalReasons.ToArray().ShouldEqual(_saved.Initialization.RefusalReasons.ToArray());
    [Fact] void should_keep_the_new_running_state() => _saved.RunningState.ShouldEqual(ObserverRunningState.ChangingDefinition);
}
