// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing.when_a_client_replica_disconnects_from_a_reactor_and_reducer.context;
using KernelConnectionId = KernelConcepts::Cratis.Chronicle.Concepts.Clients.ConnectionId;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing;

[Collection(ChronicleCollection.Name)]
public class when_a_client_replica_disconnects_from_a_reactor_and_reducer(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public const int EventsPerRound = 6;
        const string Source = "served-takeover";
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

        public IReadOnlyList<ServedDelivery> Reacted { get; private set; } = [];
        public IReadOnlyList<ServedDelivery> Reduced { get; private set; } = [];
        public ServedReadModel ReadModel { get; private set; } = default!;

        public async ValueTask InitializeAsync()
        {
            var signal = fixture.ServedSignal;
            signal.Reset();

            var eventStore = fixture.ClientEventStore;
            _ = fixture.SecondClientEventStore;
            var reactorHandler = eventStore.Reactors.GetHandlerFor<ServedReactor>();
            var reducerHandler = eventStore.Reducers.GetHandlerFor<ServedReducer>();
            await reactorHandler.WaitTillActive(_timeout);
            await reducerHandler.WaitTillActive(_timeout);

            var reactorObserver = fixture.GetObserverFor<ServedReactor>();
            var reducerObserver = fixture.GetObserverFor<ServedReducer>();
            await fixture.WaitForFanOutTargets(reactorObserver, expected: 2, _timeout);
            await fixture.WaitForFanOutTargets(reducerObserver, expected: 2, _timeout);

            for (var sequence = 1; sequence <= EventsPerRound; sequence++)
            {
                await eventStore.EventLog.Append(Source, new ServedEvent(sequence));
            }

            await signal.WaitForDeliveries(EventsPerRound, _timeout);

            // The second client instance's observe streams end - what the kernel's reactor and reducer services do
            // when a client's stream is cancelled. The remaining instance has to take over.
            var secondConnection = (KernelConnectionId)fixture.SecondClientConnectionId;
            await reactorObserver.UnsubscribeIfMatchesClient(secondConnection);
            await reducerObserver.UnsubscribeIfMatchesClient(secondConnection);

            var appendResult = default(EventSequences.AppendResult)!;
            for (var sequence = EventsPerRound + 1; sequence <= EventsPerRound * 2; sequence++)
            {
                appendResult = await eventStore.EventLog.Append(Source, new ServedEvent(sequence));
            }

            await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await reducerHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await signal.WaitForDeliveries(EventsPerRound * 2, _timeout);

            Reacted = signal.Reacted;
            Reduced = signal.Reduced;
            ReadModel = await eventStore.ReadModels.GetInstanceById<ServedReadModel>(Source);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_deliver_every_event_to_the_reactor_once_in_order() => _context.Reacted.Select(_ => _.Sequence).ShouldEqual(Enumerable.Range(1, context.EventsPerRound * 2));
    [Fact] void should_deliver_every_event_to_the_reducer_once_in_order() => _context.Reduced.Select(_ => _.Sequence).ShouldEqual(Enumerable.Range(1, context.EventsPerRound * 2));
    [Fact] void should_reduce_every_event() => _context.ReadModel.Count.ShouldEqual(context.EventsPerRound * 2);
    [Fact] void should_reduce_them_in_order() => _context.ReadModel.InOrder.ShouldBeTrue();
}
