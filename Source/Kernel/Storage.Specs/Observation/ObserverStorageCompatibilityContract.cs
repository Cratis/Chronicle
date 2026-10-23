// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Provider-independent downgrade checks over persisted event-store and namespace observer state.
/// </summary>
public abstract class ObserverStorageCompatibilityContract : Specification
{
    /// <summary>
    /// Opens storage over the same event store, without cached initialization state on persistent providers.
    /// </summary>
    /// <returns>The event-store storage.</returns>
    protected abstract IEventStoreStorage OpenStore();

    /// <summary>
    /// Opens namespace observer storage over the same database.
    /// </summary>
    /// <returns>The observer state storage.</returns>
    protected abstract IObserverStateStorage OpenObservers();

    [Fact] public async Task should_stamp_and_round_trip_the_owner()
    {
        (await OpenObservers().Save(new() { Identifier = "observer", LastHandledEventSequenceNumber = 42 }, EventSequenceId.Log)).ShouldBeTrue();
        (await OpenObservers().Get("observer")).Owner.ShouldEqual(EventSequenceId.Log);
        (await OpenObservers().Get("observer")).LastHandledEventSequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)42);
    }
    [Fact] public async Task should_accept_the_same_owner()
    {
        await OpenObservers().Save(new() { Identifier = "observer" }, EventSequenceId.Log);
        (await OpenObservers().Save(new() { Identifier = "observer", LastHandledEventSequenceNumber = 43 }, EventSequenceId.Log)).ShouldBeTrue();
        (await OpenObservers().Get("observer")).LastHandledEventSequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)43);
    }
    [Fact] public async Task should_refuse_another_owner_without_changing_progress()
    {
        await OpenObservers().Save(new() { Identifier = "observer", LastHandledEventSequenceNumber = 42 }, EventSequenceId.Log);
        (await OpenObservers().Save(new() { Identifier = "observer", LastHandledEventSequenceNumber = 99 }, "another-sequence")).ShouldBeFalse();
        var stored = await OpenObservers().Get("observer");
        stored.Owner.ShouldEqual(EventSequenceId.Log);
        stored.LastHandledEventSequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)42);
    }
    [Fact] public async Task should_only_change_the_owner_when_claiming()
    {
        await OpenObservers().Save(new() { Identifier = "observer", LastHandledEventSequenceNumber = 42 }, EventSequenceId.Log);
        await OpenObservers().ClaimOwnership("observer", "another-sequence");
        var stored = await OpenObservers().Get("observer");
        stored.Owner.ShouldEqual((EventSequenceId)"another-sequence");
        stored.LastHandledEventSequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)42);
        (await OpenObservers().Save(stored with { LastHandledEventSequenceNumber = 99 }, EventSequenceId.Log)).ShouldBeFalse();
        (await OpenObservers().Save(stored with { LastHandledEventSequenceNumber = 43 }, "another-sequence")).ShouldBeTrue();
    }
    [Fact] public async Task should_not_create_state_when_claiming_a_missing_observer()
    {
        await OpenObservers().ClaimOwnership("observer", "another-sequence");
        (await OpenObservers().GetAll()).ShouldBeEmpty();
    }
    [Fact] public async Task should_allow_a_missing_legacy_observer() => (await Check()).ShouldBeNull();
    [Fact] public async Task should_allow_store_setup_before_observer_enablement()
    {
        await OpenStore().MarkInitialized();
        (await Check()).ShouldBeNull();
    }
    [Fact] public async Task should_allow_a_refused_initialization_attempt()
    {
        await OpenObservers().Save(new() { Identifier = "observer", Initialization = new(ObserverInitializationState.NotInitialized, ["unfinished job"]) });
        (await Check()).ShouldBeNull();
    }
    [Fact] public async Task should_allow_an_observer_awaiting_registration()
    {
        await OpenObservers().Save(new() { Identifier = "observer", Initialization = new(ObserverInitializationState.AwaitingRegistration, []) });
        (await Check()).ShouldBeNull();
    }
    [Fact] public async Task should_refuse_an_initialized_store_without_a_control_record()
    {
        await OpenStore().MarkObserversInitialized();
        (await Check()).ShouldBeOfExactType<ObserverKernelBelowDowngradeFloor>();
    }
    [Fact] public async Task should_refuse_an_initialized_control_in_an_uninitialized_store()
    {
        await SaveControl();
        (await Check()).ShouldBeOfExactType<ObserverKernelBelowDowngradeFloor>();
    }
    [Fact] public async Task should_refuse_an_initializing_observer_without_a_control_record()
    {
        await SaveInitialization(ObserverInitializationState.Initializing);
        (await Check()).ShouldBeOfExactType<ObserverKernelBelowDowngradeFloor>();
    }
    [Fact] public async Task should_refuse_an_initialized_observer_without_a_control_record()
    {
        await SaveInitialization(ObserverInitializationState.Initialized);
        (await Check()).ShouldBeOfExactType<ObserverKernelBelowDowngradeFloor>();
    }
    [Fact] public async Task should_allow_the_gate_kernel_to_read_an_initializing_observer()
    {
        await SaveInitialization(ObserverInitializationState.Initializing);
        (await Check(true)).ShouldBeNull();
    }
    [Fact] public async Task should_allow_the_gate_kernel_to_read_an_initialized_observer()
    {
        await SaveInitialization(ObserverInitializationState.Initialized);
        (await Check(true)).ShouldBeNull();
    }
    [Fact] public async Task should_allow_the_gate_kernel_to_read_an_initialized_store()
    {
        await OpenStore().MarkObserversInitialized();
        (await Check(true)).ShouldBeNull();
    }
    [Fact] public async Task should_allow_the_gate_kernel_to_read_an_initialized_control()
    {
        await SaveControl();
        (await Check(true)).ShouldBeNull();
    }
    [Fact] public async Task should_not_write_markers_or_control_while_checking_legacy_state()
    {
        await Check();
        (await OpenStore().AreObserversInitialized()).ShouldBeFalse();
        (await OpenObservers().Get("observer")).TypeSetControl.ShouldBeNull();
    }
    [Fact] public async Task should_keep_initialized_state_after_a_refusal()
    {
        await SaveControl();
        await OpenStore().MarkObserversInitialized();
        await Check();
        (await OpenStore().AreObserversInitialized()).ShouldBeTrue();
        (await OpenObservers().Get("observer")).TypeSetControl!.Revision.Value.ShouldEqual(1L);
    }

    Task SaveInitialization(ObserverInitializationState state) => OpenObservers().Save(new()
    {
        Identifier = "observer",
        Initialization = new(state, [])
    });

    Task SaveControl() => OpenObservers().Save(new()
    {
        Identifier = "observer",
        TypeSetControl = new(new(1), new("reactor", ObserverFilters.None), new Dictionary<Concepts.Events.EventTypeId, ObserverTypeSetMember>(), [])
    });

    async Task<Exception> Check(bool supportsInitializedObservers = false)
    {
        var state = await OpenObservers().Get("observer");
        return await Catch.Exception(() => OpenStore().EnsureObserverCompatibility(state, supportsInitializedObservers));
    }
}
