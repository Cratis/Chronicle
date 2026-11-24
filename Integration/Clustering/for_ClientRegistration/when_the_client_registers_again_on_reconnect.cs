// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Storage;
using ConceptsEventStoreName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientRegistration.when_the_client_registers_again_on_reconnect.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// A client registers everything it has on every start and reconnect. Doing it again, with every artifact unchanged,
/// returns and leaves what the kernel holds exactly as it was.
/// </summary>
/// <param name="_context">The <see cref="context"/> for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class when_the_client_registers_again_on_reconnect(context _context) : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public int ProjectionsBefore { get; private set; }
        public int ProjectionsAfter { get; private set; }
        public int ReadModelsBefore { get; private set; }
        public int ReadModelsAfter { get; private set; }
        public int ObserversBefore { get; private set; }
        public int ObserversAfter { get; private set; }

        public async ValueTask InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            await eventStore.RegisterAll();
            var store = fixture.SiloServices.GetRequiredService<IStorage>().GetEventStore((ConceptsEventStoreName)Constants.EventStore);
            ProjectionsBefore = (await store.Projections.GetAll()).Count();
            ReadModelsBefore = (await store.ReadModels.GetAll()).Count();
            ObserversBefore = (await store.Reactors.GetAll()).Count() + (await store.Reducers.GetAll()).Count();

            // What the client does for each artifact kind when it reconnects.
            await eventStore.EventTypes.Register();
            await eventStore.ReadModels.Register();
            await eventStore.Constraints.Register();
            await eventStore.Projections.Register();

            ProjectionsAfter = (await store.Projections.GetAll()).Count();
            ReadModelsAfter = (await store.ReadModels.GetAll()).Count();
            ObserversAfter = (await store.Reactors.GetAll()).Count() + (await store.Reducers.GetAll()).Count();
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_have_projections() => _context.ProjectionsBefore.ShouldBeGreaterThan(0);
    [Fact] void should_keep_the_projections() => _context.ProjectionsAfter.ShouldEqual(_context.ProjectionsBefore);
    [Fact] void should_keep_the_read_models() => _context.ReadModelsAfter.ShouldEqual(_context.ReadModelsBefore);
    [Fact] void should_keep_the_observers() => _context.ObserversAfter.ShouldEqual(_context.ObserversBefore);
}
