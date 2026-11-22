// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using Cratis.Chronicle.Storage;
using KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelConcepts::Cratis.Chronicle.Concepts.EventTypes;
using KernelCore::Cratis.Chronicle.EventTypes;
using context = Cratis.Chronicle.Integration.Clustering.for_EventTypeRegistry.when_an_event_type_is_registered_on_one_silo.context;
using ConceptsEventStoreName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName;
using ConceptsJsonSchema = Cratis.Chronicle.Schemas.JsonSchema;

namespace Cratis.Chronicle.Integration.Clustering.for_EventTypeRegistry;

/// <summary>
/// Every silo resolves event types from its own registry. Registering an event type on one silo has to reach the registry
/// of the other, through the announcement the registering silo makes to every silo of the cluster, and the registry
/// must never serve a schema older than the announced registration afterwards.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class when_an_event_type_is_registered_on_one_silo(context _context) : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public EventTypeSchema ObserverSiloAfterFirstRegistration { get; private set; } = default!;
        public EventTypeSchema ObserverSiloAfterChange { get; private set; } = default!;
        public bool EventSequencesSiloIsAnotherRegistry { get; private set; }

        public async ValueTask InitializeAsync()
        {
            var eventStore = (ConceptsEventStoreName)Constants.EventStore;
            var registering = fixture.SiloServices.GetRequiredService<IStorage>().GetEventStore(eventStore).EventTypes;
            var observing = fixture.ObserversSiloServices.GetRequiredService<IStorage>().GetEventStore(eventStore).EventTypes;
            var announcer = fixture.SiloServices.GetRequiredService<IEventTypesCacheClient>();
            EventSequencesSiloIsAnotherRegistry = !ReferenceEquals(registering, observing);

            var eventTypeId = new EventTypeId($"ClusterRegistered-{Guid.NewGuid():N}");
            var eventType = new EventType(eventTypeId, 1);

            // Warm the observing silo's registry before anything is registered, so it holds a snapshot that is older
            // than the registration.
            await observing.GetLatestForAllEventTypes();

            await registering.Register(eventType, await ConceptsJsonSchema.FromJsonAsync("""{"type":"object","properties":{"first":{"type":"string"}}}"""));
            await announcer.Invalidate(eventStore, eventTypeId);
            ObserverSiloAfterFirstRegistration = await observing.GetFor(eventTypeId, 1);

            await registering.Register(eventType, await ConceptsJsonSchema.FromJsonAsync("""{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"string"}}}"""));
            await announcer.Invalidate(eventStore, eventTypeId);
            ObserverSiloAfterChange = await observing.GetFor(eventTypeId, 1);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_have_a_registry_per_silo() => _context.EventSequencesSiloIsAnotherRegistry.ShouldBeTrue();
    [Fact] void should_resolve_the_new_event_type_on_the_other_silo() => _context.ObserverSiloAfterFirstRegistration.Schema.Properties.ContainsKey("first").ShouldBeTrue();
    [Fact] void should_resolve_the_changed_schema_on_the_other_silo() => _context.ObserverSiloAfterChange.Schema.Properties.ContainsKey("second").ShouldBeTrue();
}
