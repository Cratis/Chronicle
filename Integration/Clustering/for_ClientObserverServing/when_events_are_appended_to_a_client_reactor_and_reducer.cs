// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing.when_events_are_appended_to_a_client_reactor_and_reducer.context;

namespace Cratis.Chronicle.Integration.Clustering.for_ClientObserverServing;

[Collection(ChronicleCollection.Name)]
public class when_events_are_appended_to_a_client_reactor_and_reducer(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        public const int Events = 8;
        const string Source = "served-in-order";
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

        public IReadOnlyList<ServedDelivery> Reacted { get; private set; } = [];
        public IReadOnlyList<ServedDelivery> Reduced { get; private set; } = [];
        public ServedReadModel ReadModel { get; private set; } = default!;

        public async ValueTask InitializeAsync()
        {
            var signal = fixture.ServedSignal;
            signal.Reset();

            var eventStore = fixture.ClientEventStore;
            var reactorHandler = eventStore.Reactors.GetHandlerFor<ServedReactor>();
            var reducerHandler = eventStore.Reducers.GetHandlerFor<ServedReducer>();
            await reactorHandler.WaitTillActive(_timeout);
            await reducerHandler.WaitTillActive(_timeout);

            var appendResult = default(EventSequences.AppendResult)!;
            for (var sequence = 1; sequence <= Events; sequence++)
            {
                appendResult = await eventStore.EventLog.Append(Source, new ServedEvent(sequence));
            }

            await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await reducerHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await signal.WaitForDeliveries(Events, _timeout);

            Reacted = signal.Reacted;
            Reduced = signal.Reduced;
            ReadModel = await eventStore.ReadModels.GetInstanceById<ServedReadModel>(Source);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact] void should_deliver_every_event_to_the_reactor_in_order() => _context.Reacted.Select(_ => _.Sequence).ShouldEqual(Enumerable.Range(1, context.Events));
    [Fact] void should_deliver_every_event_to_the_reducer_in_order() => _context.Reduced.Select(_ => _.Sequence).ShouldEqual(Enumerable.Range(1, context.Events));
    [Fact] void should_reduce_every_event() => _context.ReadModel.Count.ShouldEqual(context.Events);
    [Fact] void should_reduce_them_in_order() => _context.ReadModel.InOrder.ShouldBeTrue();
}
