// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Observation.Registrations;
using Cratis.Json;

namespace Cratis.Chronicle.Storage.Observation.Registrations;

/// <summary>
/// Specifies the durable registration contract shared by every provider.
/// </summary>
public abstract partial class ObserverRegistrationsStorageContract : Specification
{
    protected static readonly JsonSerializerOptions SerializerOptions = new() { Converters = { new ConceptAsJsonConverterFactory() } };

    /// <summary>
    /// Opens another storage instance over the same event store.
    /// </summary>
    /// <returns>The registrations storage.</returns>
    protected abstract IObserverRegistrationsStorage Open();

    /// <summary>
    /// Opens registrations in a different event store.
    /// </summary>
    /// <returns>The isolated storage.</returns>
    protected abstract Task<IObserverRegistrationsStorage> OpenOtherStore();

    protected static ObserverRegistration Registration(bool reducer = false, string sequence = "event-log")
    {
        EventTypeWithKeyExpression[] events = [new(new("orders.placed", 3), "orderId"), new(new("orders.placed", 4), "orderId")];
        var filters = new ObserverFilters(["paid"], "order", "public");
        ObserverRegistrationDefinition definition = reducer
            ? new ReducerRegistrationDefinition(new ReducerDefinition("observer", sequence, events, "orders", true, ["sales"], filters, "fingerprint") { GenerationDelivery = EventGenerationDelivery.Pinned })
            : new ReactorRegistrationDefinition(new ReactorDefinition("observer", ReactorOwner.Client, sequence, events, false, ["sales"], filters) { GenerationDelivery = EventGenerationDelivery.Pinned, StartAtTail = true });
        return new("observer", sequence, reducer ? ObserverType.Reducer : ObserverType.Reactor, definition)
        {
            Owner = new("program"),
            OwnerPrincipal = new("subject"),
            HttpTarget = new(new("https://example.com/events"), new Dictionary<string, string> { ["X-Tenant"] = "sales" }),
            TargetKind = ObserverTargetKind.Http,
            Source = ObserverRegistrationSource.ArtifactSet,
            Revision = new(1),
            DeploymentGeneration = 42,
            LastChanged = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)
        };
    }

    [Fact] public async Task should_round_trip_the_complete_reactor()
    {
        var registration = Registration();
        (await Open().TrySave(registration, new(0))).ShouldBeTrue();
        JsonSerializer.Serialize(await Open().Get(registration.ObserverId, registration.EventSequenceId), SerializerOptions).ShouldEqual(JsonSerializer.Serialize(registration, SerializerOptions));
    }
    [Fact] public async Task should_round_trip_the_complete_reducer()
    {
        var registration = Registration(true);
        (await Open().TrySave(registration, new(0))).ShouldBeTrue();
        JsonSerializer.Serialize(await Open().Get(registration.ObserverId, registration.EventSequenceId), SerializerOptions).ShouldEqual(JsonSerializer.Serialize(registration, SerializerOptions));
    }
    [Fact] public async Task should_keep_the_same_observer_on_different_sequences_separate()
    {
        await Open().TrySave(Registration(), new(0));
        await Open().TrySave(Registration(sequence: "another"), new(0));
        (await Open().Get("observer", "another"))!.EventSequenceId.ShouldEqual((EventSequenceId)"another");
        (await Open().Get("observer", "event-log"))!.EventSequenceId.ShouldEqual((EventSequenceId)"event-log");
    }
    [Fact] public async Task should_store_a_snapshot_instead_of_mutable_header_references()
    {
        var registration = Registration();
        await Open().TrySave(registration, new(0));
        registration.HttpTarget!.Headers["X-Tenant"] = "changed";
        var firstRead = (await Open().Get(registration.ObserverId, registration.EventSequenceId))!;
        firstRead.HttpTarget!.Headers["X-Tenant"].ShouldEqual("sales");
        firstRead.HttpTarget.Headers["X-Tenant"] = "changed-again";
        (await Open().Get(registration.ObserverId, registration.EventSequenceId))!.HttpTarget!.Headers["X-Tenant"].ShouldEqual("sales");
    }
    [Fact] public async Task should_leave_missing_registrations_in_legacy_behavior() => (await Open().Get("missing", EventSequenceId.Log)).ShouldBeNull();
    [Fact] public async Task should_isolate_registrations_settings_and_floor_between_event_stores()
    {
        await Open().TrySave(Registration(), new(0));
        await Open().TrySaveGenerationFloor(new(42, 1), 0);
        await Open().TrySaveOwnerSettings(new(new("program"), new("subject")) { Revision = 1 }, 0);
        var other = await OpenOtherStore();
        (await other.Get("observer", "event-log")).ShouldBeNull();
        (await other.GetActive()).ShouldBeEmpty();
        (await other.GetByOwner(new("program"))).ShouldBeEmpty();
        (await other.GetGenerationFloor()).ShouldBeNull();
        (await other.GetOwnerSettings(new("program"))).ShouldBeNull();
    }
}
