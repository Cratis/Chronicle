// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations.and_observers_consume_second_generation.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations;

[Collection(ChronicleCollection.Name)]
public class and_observers_consume_second_generation(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "some-user";
        public UserRegisteredV1 Event { get; private set; }
        public UserRegisteredReactor Reactor { get; private set; }
        public UserRegisteredReducer Reducer { get; private set; }
        public UserReadModel? ProjectionResult { get; }

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(UserRegisteredV1),
            typeof(UserRegisteredV2),
            typeof(UserRegisteredV3)
        ];

        public override IEnumerable<Type> EventTypeMigrators =>
        [
            typeof(UserRegisteredV1ToV2Migrator),
            typeof(UserRegisteredV2ToV3Migrator)
        ];

        public override IEnumerable<Type> Reactors => [typeof(UserRegisteredReactor)];
        public override IEnumerable<Type> Reducers => [typeof(UserRegisteredReducer)];
        public override IEnumerable<Type> Projections => [typeof(UserRegisteredProjection)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new UserRegisteredReactor();
            Reducer = new UserRegisteredReducer();
            services.AddSingleton(Reactor);
            services.AddSingleton(Reducer);
            services.AddSingleton(new UserRegisteredProjection());
        }

        void Establish()
        {
            Event = new UserRegisteredV1("Jane Doe");
        }

        async Task Because()
        {
            var reactorHandler = EventStore.Reactors.GetHandlerFor<UserRegisteredReactor>();
            var reducerHandler = EventStore.Reducers.GetHandlerFor<UserRegisteredReducer>();
            var projectionHandler = EventStore.Projections.GetHandlerFor<UserRegisteredProjection>();

            await reactorHandler.WaitTillSubscribed();
            await reducerHandler.WaitTillSubscribed();
            await projectionHandler.WaitTillSubscribed();

            var result = await EventStore.EventLog.Append(EventSourceId, Event);

            await projectionHandler.WaitTillReachesEventSequenceNumber(result.SequenceNumber);
            await WaitForObservedStateBehavioral();
        }

        async Task WaitForObservedStateBehavioral(TimeSpan? timeout = default)
        {
            timeout ??= TimeSpanFactory.DefaultTimeout();
            using var cts = new CancellationTokenSource(timeout.Value);

            while (true)
            {
                if (Reactor.ReceivedGeneration == 2u &&
                    Reactor.ReceivedFirstName == "Jane" &&
                    Reactor.ReceivedLastName == "Doe" &&
                    Reducer.ReceivedGeneration == 2u &&
                    Reducer.ReceivedFirstName == "Jane" &&
                    Reducer.ReceivedLastName == "Doe")
                {
                    return;
                }

                await Task.Delay(50, cts.Token);
            }
        }
    }

    [Fact] void should_have_reactor_receive_first_name() => Context.Reactor.ReceivedFirstName.ShouldEqual("Jane");
    [Fact] void should_have_reactor_receive_last_name() => Context.Reactor.ReceivedLastName.ShouldEqual("Doe");
    [Fact] void should_have_reactor_receive_generation_2() => Context.Reactor.ReceivedGeneration.ShouldEqual(2u);
    [Fact] void should_have_reducer_receive_first_name() => Context.Reducer.ReceivedFirstName.ShouldEqual("Jane");
    [Fact] void should_have_reducer_receive_last_name() => Context.Reducer.ReceivedLastName.ShouldEqual("Doe");
    [Fact] void should_have_reducer_receive_generation_2() => Context.Reducer.ReceivedGeneration.ShouldEqual(2u);

    [Fact]
    void should_have_projection_set_first_name()
    {
        if (Context.ProjectionResult is null) return;
        Context.ProjectionResult.FirstName.ShouldEqual("Jane");
    }

    [Fact]
    void should_have_projection_set_last_name()
    {
        if (Context.ProjectionResult is null) return;
        Context.ProjectionResult.LastName.ShouldEqual("Doe");
    }
}
