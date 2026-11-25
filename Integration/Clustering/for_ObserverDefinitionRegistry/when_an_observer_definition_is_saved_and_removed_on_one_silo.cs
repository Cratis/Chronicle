// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelConcepts::Cratis.Chronicle.Concepts.EventSequences;
using KernelConcepts::Cratis.Chronicle.Concepts.Observation;
using KernelCore::Cratis.Chronicle.Observation;
using ConceptsEventStoreName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName;
using context = Cratis.Chronicle.Integration.Clustering.for_ObserverDefinitionRegistry.when_an_observer_definition_is_saved_and_removed_on_one_silo.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ObserverDefinitionRegistry;

/// <summary>
/// Every silo serves observer definitions from its own registry. Saving or removing a definition on one silo has to reach
/// the registry of the other through the announcement made to every silo of the cluster, and a removed observer must not
/// come back from a snapshot that is older than the removal.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class when_an_observer_definition_is_saved_and_removed_on_one_silo(context _context) : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public bool SilosHaveTheirOwnRegistry { get; private set; }
        public ObserverDefinition SeenAfterSave { get; private set; } = ObserverDefinition.Empty;
        public ObserverDefinition SeenAfterChange { get; private set; } = ObserverDefinition.Empty;
        public bool HasAfterRemoval { get; private set; } = true;
        public ObserverDefinition SeenAfterRemoval { get; private set; } = ObserverDefinition.Empty;

        public async ValueTask InitializeAsync()
        {
            var eventStore = (ConceptsEventStoreName)Constants.EventStore;
            var registering = fixture.SiloServices.GetRequiredService<IStorage>().GetEventStore(eventStore).Observers;
            var observing = fixture.ObserversSiloServices.GetRequiredService<IStorage>().GetEventStore(eventStore).Observers;
            var announcer = fixture.SiloServices.GetRequiredService<IObserverDefinitionsCacheClient>();
            SilosHaveTheirOwnRegistry = !ReferenceEquals(registering, observing);

            var observerId = new ObserverId($"ClusterObserver-{Guid.NewGuid():N}");

            // Warm the observing silo's registry before anything is saved, so it holds a snapshot older than the save.
            await observing.GetAll();

            await registering.Save(DefinitionOf(observerId, new EventType("first-event", 1)));
            await announcer.Invalidate(eventStore, observerId);
            SeenAfterSave = await observing.Get(observerId);

            await registering.Save(DefinitionOf(observerId, new EventType("second-event", 1)));
            await announcer.Invalidate(eventStore, observerId);
            SeenAfterChange = await observing.Get(observerId);

            await registering.Delete(observerId);
            await announcer.Invalidate(eventStore, observerId);
            HasAfterRemoval = await observing.Has(observerId);
            SeenAfterRemoval = await observing.Get(observerId);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        static ObserverDefinition DefinitionOf(ObserverId observerId, EventType eventType) =>
            new(observerId, [eventType], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true);
    }

    [Fact] void should_have_a_registry_per_silo() => _context.SilosHaveTheirOwnRegistry.ShouldBeTrue();
    [Fact] void should_serve_the_new_definition_on_the_other_silo() => _context.SeenAfterSave.EventTypes.Single().Id.Value.ShouldEqual("first-event");
    [Fact] void should_serve_the_changed_definition_on_the_other_silo() => _context.SeenAfterChange.EventTypes.Single().Id.Value.ShouldEqual("second-event");
    [Fact] void should_not_have_the_removed_definition_on_the_other_silo() => _context.HasAfterRemoval.ShouldBeFalse();
    [Fact] void should_not_bring_the_removed_definition_back_on_the_other_silo() => _context.SeenAfterRemoval.ShouldEqual(ObserverDefinition.Empty);
}
