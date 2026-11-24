// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing.when_serving_a_client_across_silos.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing;

/// <summary>
/// An observer is placed on any silo that hosts observers, while the stream of the client it serves ends on one silo. The
/// subscriber grain that bridges the two is placed on the silo named in its own key (the silo that holds the stream), so
/// delivery reaches the client whichever silo the observer is on (#6121).
/// </summary>
/// <param name="_context">The context.</param>
[Collection(ChronicleCollection.Name)]
public class when_serving_a_client_across_silos(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        const int Events = 3;
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

        public int Delivered { get; private set; }
        public int Subscribers { get; private set; }
        public int SubscribersOnTheSiloOfTheirKey { get; private set; }

        public async ValueTask InitializeAsync()
        {
            fixture.ServedSignal.Reset();
            var eventStore = fixture.ClientEventStore;
            var handler = eventStore.Reactors.GetHandlerFor<ServedReactor>();
            await handler.WaitTillActive(_timeout);
            await eventStore.Reducers.GetHandlerFor<ServedReducer>().WaitTillActive(_timeout);

            var appendResult = default(EventSequences.AppendResult)!;
            for (var sequence = 1; sequence <= Events; sequence++)
            {
                appendResult = await eventStore.EventLog.Append("served-across-silos", new ServedEvent(sequence));
            }

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await fixture.ServedSignal.WaitForDeliveries(Events, _timeout);
            Delivered = fixture.ServedSignal.Reacted.Count;

            var management = fixture.SiloServices.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0);
            var subscribers = (await management.GetDetailedGrainStatistics())
                .Where(_ => _.GrainType.StartsWith("Cratis.Chronicle.Observation.Reactors.Clients.ReactorObserverSubscriber,", StringComparison.Ordinal) ||
                            _.GrainType.StartsWith("Cratis.Chronicle.Observation.Reducers.Clients.ReducerObserverSubscriber,", StringComparison.Ordinal))
                .ToArray();
            Subscribers = subscribers.Length;
            SubscribersOnTheSiloOfTheirKey = subscribers.Count(_ => _.GrainId.ToString().Contains(_.SiloAddress.ToParsableString(), StringComparison.Ordinal));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_deliver_every_event_to_the_client() => _context.Delivered.ShouldBeGreaterThanOrEqual(3);
    [Fact] void should_have_activated_subscribers() => _context.Subscribers.ShouldBeGreaterThan(0);
    [Fact] void should_place_every_subscriber_on_the_silo_named_in_its_key() => _context.SubscribersOnTheSiloOfTheirKey.ShouldEqual(_context.Subscribers);
}
