// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Storage.Observation;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_system_event_types_are_needed_by_observer_alerts : given.a_startup_task
{
    bool _registered;
    bool _registeredBeforePatches;
    bool _registeredBeforeSystemReactors;
    bool _registeredBeforeAlerts;
    IObserver _observer;

    void Establish()
    {
        // System is deliberately absent from GetEventStores: registration cannot depend on store enumeration order.
        _eventTypes.DiscoverAndRegister(EventStoreName.System).Returns(_ =>
        {
            _registered = true;
            return Task.CompletedTask;
        });
        _patchManager.ApplyPatchesFor(Arg.Any<Cratis.Chronicle.Concepts.System.SemanticVersion>()).Returns(_ =>
        {
            _registeredBeforePatches = _registered;
            return Task.FromResult(PatchOutcome.Completed);
        });
        _reactors.DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default).Returns(_ =>
        {
            _registeredBeforeSystemReactors = _registered;
            return Task.CompletedTask;
        });
        _observer = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(new ObserverKey("projection", _eventStore, _namespace, EventSequenceId.Log)).Returns(_observer);
        _observer.Ensure().Returns(_ =>
        {
            _registeredBeforeAlerts = _registered;
            return Task.CompletedTask;
        });
        _observerStateStorage.GetAll().Returns([ObserverState.Empty with { Identifier = "projection" }]);
        _eventStoreStorage.Observers.GetAll().Returns([new ObserverDefinition { Identifier = "projection", EventSequenceId = EventSequenceId.Log }]);
    }

    Task Because() => Execute();

    [Fact] void should_register_before_patches_can_activate_grains() => _registeredBeforePatches.ShouldBeTrue();
    [Fact] void should_register_before_system_reactors_activate() => _registeredBeforeSystemReactors.ShouldBeTrue();
    [Fact] void should_register_before_application_observer_alerts_bootstrap() => _registeredBeforeAlerts.ShouldBeTrue();
    [Fact] async Task should_bootstrap_the_observer() => await _observer.Received(1).Ensure();
}
