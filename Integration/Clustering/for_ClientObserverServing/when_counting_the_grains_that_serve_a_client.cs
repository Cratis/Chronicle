// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing.when_counting_the_grains_that_serve_a_client.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing;

/// <summary>
/// Pins today's design, a grain per connected client observer; removing the grains flips both counts to zero.
/// </summary>
/// <param name="_context">The context.</param>
[Collection(ChronicleCollection.Name)]
public class when_counting_the_grains_that_serve_a_client(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

        public int ClientReactorGrains { get; private set; }
        public int ClientReducerGrains { get; private set; }

        public async ValueTask InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            await eventStore.Reactors.GetHandlerFor<ServedReactor>().WaitTillActive(_timeout);
            await eventStore.Reducers.GetHandlerFor<ServedReducer>().WaitTillActive(_timeout);

            var management = fixture.SiloServices.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0);
            var statistics = await management.GetDetailedGrainStatistics();
            ClientReactorGrains = statistics.Count(_ => _.GrainType.StartsWith("Cratis.Chronicle.Observation.Reactors.Clients.Reactor,", StringComparison.Ordinal));
            ClientReducerGrains = statistics.Count(_ => _.GrainType.StartsWith("Cratis.Chronicle.Observation.Reducers.Clients.Reducer,", StringComparison.Ordinal));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_serve_client_reactors_with_grains() => _context.ClientReactorGrains.ShouldBeGreaterThan(0);
    [Fact] void should_serve_client_reducers_with_grains() => _context.ClientReducerGrains.ShouldBeGreaterThan(0);
}
