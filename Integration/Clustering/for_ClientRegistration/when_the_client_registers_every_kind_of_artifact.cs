// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Storage;
using ConceptsEventStoreName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientRegistration.when_the_client_registers_every_kind_of_artifact.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// Pins what client registration puts in the kernel's storage today: event types, constraints, reactors, reducers,
/// read models and projections the client discovers are all present once registration has returned.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class when_the_client_registers_every_kind_of_artifact(context _context) : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public bool HasEventType { get; private set; }
        public bool HasConstraint { get; private set; }
        public bool HasReactor { get; private set; }
        public bool HasReducer { get; private set; }
        public bool HasReadModel { get; private set; }
        public bool HasProjection { get; private set; }

        public async ValueTask InitializeAsync()
        {
            await fixture.ClientEventStore.RegisterAll();
            var store = fixture.SiloServices.GetRequiredService<IStorage>().GetEventStore((ConceptsEventStoreName)Constants.EventStore);
            HasEventType = (await store.EventTypes.GetAllDefinitions()).Any(_ => _.Id.Value.Contains(nameof(RegistrationAuditEvent)));
            HasConstraint = (await store.Constraints.GetDefinitions()).Any(_ => _.Name.Value.Contains(nameof(UniqueRegistrationAuditReference)));
            HasReactor = (await store.Reactors.GetAll()).Any(_ => _.Identifier.Value.Contains("ClusteredReactor"));
            HasReducer = (await store.Reducers.GetAll()).Any(_ => _.Identifier.Value.Contains("ClusteredReducer"));
            HasReadModel = (await store.ReadModels.GetAll()).Any(_ => _.ContainerName.Value.Contains("ClusteredProjectionReadModel", StringComparison.OrdinalIgnoreCase) || _.Identifier.Value.Contains("ClusteredProjectionReadModel"));
            HasProjection = (await store.Projections.GetAll()).Any(_ => _.Identifier.Value.EndsWith(nameof(for_Clustering.ClusteredProjection), StringComparison.Ordinal));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_have_registered_the_event_type() => _context.HasEventType.ShouldBeTrue();
    [Fact] void should_have_registered_the_constraint() => _context.HasConstraint.ShouldBeTrue();
    [Fact] void should_have_registered_the_reactor() => _context.HasReactor.ShouldBeTrue();
    [Fact] void should_have_registered_the_reducer() => _context.HasReducer.ShouldBeTrue();
    [Fact] void should_have_registered_the_read_model() => _context.HasReadModel.ShouldBeTrue();
    [Fact] void should_have_registered_the_projection() => _context.HasProjection.ShouldBeTrue();
}
