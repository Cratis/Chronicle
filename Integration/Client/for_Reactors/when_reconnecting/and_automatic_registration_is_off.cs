// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_reconnecting.and_automatic_registration_is_off.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_reconnecting;

[Collection(ChronicleCollection.Name)]
public class and_automatic_registration_is_off(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
#pragma warning disable CA2213 // Destroy disposes the client through the specification lifecycle.
        ChronicleClient _client;
#pragma warning restore CA2213
        IEventStore _store;
        public ReactorWithoutDelay Reactor;
        public IReactorHandler Original;
        public IReactorHandler Handler;
        public ConnectionId OriginalConnectionId;
        public ReactorState ReactorState;
        public EventSequenceNumber SecondAppendSequenceNumber;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new();
            services.AddSingleton(Reactor);
        }

        async Task Establish()
        {
            _client = new ChronicleClient(
                new BorrowedConnection(EventStore.Connection),
                new ChronicleOptions
                {
                    AutoDiscoverAndRegister = false,
                    DefaultSinkTypeId = Services.GetRequiredService<IChronicleClient>().Options.DefaultSinkTypeId
                },
                artifactsProvider: new ExplicitArtifacts(),
                serviceProvider: Services);
            _store = await _client.GetEventStore(EventStore.Name, EventStore.Namespace);
            await _store.DiscoverAll();
            await _store.RegisterAll();
            Original = await _store.Reactors.Register<ReactorWithoutDelay>();
            await Original.WaitTillActive();
        }

        async Task Because()
        {
            var firstAppendResult = await _store.EventLog.Append("reactor", new SomeEvent(1));
            await Original.WaitTillReachesEventSequenceNumber(firstAppendResult.SequenceNumber);
            await Reactor.WaitTillHandledEventReaches(1);

            OriginalConnectionId = EventStore.Connection.Lifecycle.ConnectionId;
            await EventStore.Connection.Lifecycle.Disconnected();
            await Original.WaitForState(ObserverRunningState.Disconnected, TimeSpan.FromSeconds(30));
            await EventStore.Connection.Connect();

            Handler = _store.Reactors.GetHandlerFor<ReactorWithoutDelay>();
            await Handler.WaitTillActive(TimeSpan.FromSeconds(30));
            var secondAppendResult = await _store.EventLog.Append("reactor", new SomeEvent(2));
            SecondAppendSequenceNumber = secondAppendResult.SequenceNumber;
            await Handler.WaitTillReachesEventSequenceNumber(SecondAppendSequenceNumber);
            await Reactor.WaitTillHandledEventReaches(2);
            ReactorState = await Handler.WaitTillActiveAndGetState(TimeSpan.FromSeconds(30));
        }

        void Destroy()
        {
            var handler = Handler ?? Original;
            if (handler is not null)
            {
                _store.Reactors.Unregister(handler.Id);
            }
            _client?.EvictEventStores();
            _client?.Dispose();
        }

        sealed class ExplicitArtifacts() : DefaultClientArtifactsProvider(new NoAssemblies())
        {
            public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
            public override IEnumerable<Type> Reactors => [];
        }

        sealed class NoAssemblies : ICanProvideAssembliesForDiscovery
        {
            public IEnumerable<Assembly> Assemblies => [];
            public IEnumerable<Type> DefinedTypes => [];

            public void Initialize()
            {
            }
        }
    }

    [Fact] void should_get_a_new_connection() => Context.EventStore.Connection.Lifecycle.ConnectionId.ShouldNotEqual(Context.OriginalConnectionId);
    [Fact] void should_hand_out_a_recreated_handler() => ReferenceEquals(Context.Original, Context.Handler).ShouldBeFalse();
    [Fact] void should_have_the_reactor_active() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_handle_the_event_appended_after_reconnect() => Context.Reactor.HandledEvents.ShouldEqual(2);
    [Fact] void should_have_reached_the_last_appended_event() => Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.SecondAppendSequenceNumber.Value);
}
