// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_reading_observer_information.and_reads_overlap_an_observe_stream_reconnection.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_reading_observer_information;

[Collection(ChronicleCollection.Name)]
public class and_reads_overlap_an_observe_stream_reconnection(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public const int Rounds = 4;
        public const int Readers = 16;
        public ReactorWithoutDelay Reactor;
        public List<ReactorState> States = [];
        public List<Exception> Errors = [];
        public ReactorState FinalState;
        public EventSequenceNumber LastSequenceNumber;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorWithoutDelay)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new();
            services.AddSingleton(Reactor);
        }

        async Task Because()
        {
            using var client = CreateClientForOutOfProcessKernel();
            var eventStore = EventStore;
            if (client is not null)
            {
                eventStore = await client.GetEventStore(EventStore.Name, EventStore.Namespace);
                await eventStore.DiscoverAll();
                await eventStore.RegisterAll();
            }

            var handler = eventStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
            await handler.WaitTillActive();
            var firstAppend = await eventStore.EventLog.Append("reactor", new SomeEvent(1));
            await handler.WaitTillReachesEventSequenceNumber(firstAppend.SequenceNumber);
            await ReadRoundsAcrossReconnection(eventStore);

            handler = eventStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
            var lastAppend = await eventStore.EventLog.Append("reactor", new SomeEvent(2));
            LastSequenceNumber = lastAppend.SequenceNumber;
            await handler.WaitTillReachesEventSequenceNumber(LastSequenceNumber);
            FinalState = await handler.WaitTillActiveAndGetState();
        }

        async Task ReadRoundsAcrossReconnection(IEventStore eventStore)
        {
            for (var round = 0; round < Rounds; round++)
            {
                var handler = eventStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
                await handler.WaitTillActive();
                var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var reads = Enumerable.Range(0, Readers).Select(_ => ReadState(handler, start.Task)).ToArray();
                var reconnection = Reconnect(eventStore.Connection, start.Task);
                start.SetResult();
                var allReads = Task.WhenAll(reads);
                await Task.WhenAll(allReads, reconnection).WaitAsync(TimeSpanFactory.FromSeconds(30));
                var results = await allReads;
                States.AddRange(results.Select(_ => _.State).OfType<ReactorState>());
                Errors.AddRange(results.Select(_ => _.Error).OfType<Exception>());
            }
        }

        static async Task Reconnect(IChronicleConnection connection, Task start)
        {
            await start;
            await connection.Lifecycle.Disconnected();
            await connection.Connect();
        }

        static async Task<(ReactorState? State, Exception? Error)> ReadState(IReactorHandler handler, Task start)
        {
            await start;
            try
            {
                // GetState calls the real unary GetObserverInformation operation while Observe is subscribed.
                return (await handler.GetState(), null);
            }
            catch (Exception error)
            {
                return (null, error);
            }
        }

        ChronicleClient? CreateClientForOutOfProcessKernel()
        {
            if (ChronicleFixture is not ChronicleFixture configurable || configurable.Options.Mode != ChronicleRuntimeMode.OutOfProcess)
            {
                return null;
            }

            return new(
                new ChronicleClientOptions
                {
                    ConnectionString = new ChronicleConnectionString($"chronicle://localhost:{configurable.KernelGrpcHostPort}/?skipTlsValidation=true"),
                    EventStore = EventStore.Name,
                    AutoDiscoverAndRegister = false
                },
                artifactsProvider: this,
                serviceProvider: Services);
        }
    }

    [Fact] void should_complete_every_information_read() => Context.States.Count.ShouldEqual(context.Rounds * context.Readers);
    [Fact] void should_not_fail_any_information_read() => Context.Errors.ShouldBeEmpty();
    [Fact] void should_return_information_for_the_registered_observer() => Context.States.TrueForAll(_ => _.Id == Context.FinalState.Id).ShouldBeTrue();
    [Fact] void should_remain_active() => Context.FinalState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_remain_subscribed() => Context.FinalState.IsSubscribed.ShouldBeTrue();
    [Fact] void should_handle_events_after_reconnecting() => Context.FinalState.LastHandledEventSequenceNumber.ShouldEqual(Context.LastSequenceNumber);
    [Fact] void should_deliver_events_to_the_reactor() => Context.Reactor.HandledEvents.ShouldBeGreaterThanOrEqual(2);
}
