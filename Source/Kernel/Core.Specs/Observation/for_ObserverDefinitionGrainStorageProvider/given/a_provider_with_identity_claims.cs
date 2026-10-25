// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverDefinitionGrainStorageProvider.given;

public class a_provider_with_identity_claims : Specification
{
    protected ObserverDefinitionGrainStorageProvider provider;
    protected IStorage storage;
    protected IEventStoreStorage eventStoreStorage;
    protected IObserverDefinitionsStorage observers;
    protected IGenerationEnablement enablement;
    protected IObserverIdentityClaims claims;
    protected IGenerationBuildStorage builds;
    protected GrainId grainId;
    protected ObserverKey observerKey;
    protected ObserverDefinition definition;
    protected IGrainState<ObserverDefinition> state;
    protected ObserverIdentityClaim registration;
    protected GenerationObserverBinding binding;
    protected ObserverIdentityClaim generation;

    void Establish()
    {
        var identity = MaterializationGenerationIdentity.For(new ProjectionId("projection"), new MaterializationGeneration(2), "models");
        observerKey = new(identity.ObserverId, "store", "namespace", EventSequenceId.Log);
        grainId = GrainId.Create("type", observerKey.ToString());
        definition = new() { Identifier = observerKey.ObserverId, EventSequenceId = EventSequenceId.Log, Type = ObserverType.Projection };
        state = new GrainState<ObserverDefinition> { State = definition };
        registration = ObserverIdentityClaim.ForRegistration(observerKey.ObserverId);
        binding = new(new(new(Guid.NewGuid()), "namespace", "projection", "mongodb", SinkConfigurationId.None), identity, EventSequenceId.Log, "revision");
        generation = ObserverIdentityClaim.ForGeneration(binding);

        storage = Substitute.For<IStorage>();
        eventStoreStorage = Substitute.For<IEventStoreStorage>();
        observers = Substitute.For<IObserverDefinitionsStorage>();
        enablement = Substitute.For<IGenerationEnablement>();
        enablement.Enabled.Returns(true);
        claims = Substitute.For<IObserverIdentityClaims>();
        builds = Substitute.For<IGenerationBuildStorage>();
        builds.ResolveObserver(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EventSequenceId>(), Arg.Any<ObserverId>()).Returns((GenerationObserverBinding?)null);
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.Observers.Returns(observers);
        provider = new(storage, enablement, claims, builds);
    }
}
