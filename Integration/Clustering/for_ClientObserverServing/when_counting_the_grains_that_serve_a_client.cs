// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing.when_counting_the_grains_that_serve_a_client.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing;

/// <summary>
/// A connected client reactor or reducer is served by its connection's stream, so it holds no grain activation.
/// </summary>
/// <param name="_context">The context.</param>
[Collection(ChronicleCollection.Name)]
public class when_counting_the_grains_that_serve_a_client(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

        public int ObserverGrains { get; private set; }
        public IReadOnlyList<string> ClientGrainsThatAreNotSubscribers { get; private set; } = [];

        public async ValueTask InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            await eventStore.Reactors.GetHandlerFor<ServedReactor>().WaitTillActive(_timeout);
            await eventStore.Reducers.GetHandlerFor<ServedReducer>().WaitTillActive(_timeout);

            var management = fixture.SiloServices.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0);
            var statistics = await management.GetDetailedGrainStatistics();
            var types = statistics.Select(_ => _.GrainType).ToArray();
            ObserverGrains = types.Count(_ => _.StartsWith("Cratis.Chronicle.Observation.Observer,", StringComparison.Ordinal));
            ClientGrainsThatAreNotSubscribers = [.. types
                .Where(_ => (_.StartsWith("Cratis.Chronicle.Observation.Reactors.Clients.", StringComparison.Ordinal) || _.StartsWith("Cratis.Chronicle.Observation.Reducers.Clients.", StringComparison.Ordinal))
                    && !_.Contains("ObserverSubscriber", StringComparison.Ordinal))
                .Distinct()];
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_have_activated_the_observers() => _context.ObserverGrains.ShouldBeGreaterThan(0);
    [Fact] void should_not_activate_a_grain_for_a_client_reactor_or_reducer() => _context.ClientGrainsThatAreNotSubscribers.ShouldBeEmpty();
}
