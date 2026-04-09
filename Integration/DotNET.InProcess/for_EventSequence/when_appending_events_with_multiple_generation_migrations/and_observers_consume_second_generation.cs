// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations.and_observers_consume_second_generation.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations;

[Collection(ChronicleCollection.Name)]
public class and_observers_consume_second_generation(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        readonly ChronicleInProcessFixture _fixture = chronicleInProcessFixture;
#pragma warning restore CA2213 // Disposable fields should be disposed

        public EventSourceId EventSourceId { get; } = "some-user";
        public UserRegisteredV1 Event { get; private set; }
        public UserRegisteredReactor Reactor { get; private set; }
        public UserRegisteredReducer Reducer { get; private set; }
        public BsonDocument StoredEvent { get; private set; }
        public UserReadModel ProjectionResult { get; private set; }

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

            await reactorHandler.WaitTillActive();
            await reducerHandler.WaitTillActive();
            await projectionHandler.WaitTillActive();

            await EventStore.EventLog.Append(EventSourceId, Event);

            await reactorHandler.WaitTillReachesEventSequenceNumber(EventSequenceNumber.First);
            await reducerHandler.WaitTillReachesEventSequenceNumber(EventSequenceNumber.First);
            await projectionHandler.WaitTillReachesEventSequenceNumber(EventSequenceNumber.First);

            var collection = EventStoreForNamespaceDatabase.Database.GetCollection<BsonDocument>("event-log");
            StoredEvent = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();

            var filter = Builders<UserReadModel>.Filter.Eq(new StringFieldDefinition<UserReadModel, string>("_id"), EventSourceId.Value);
            var result = await _fixture.ReadModels.Database.GetCollection<UserReadModel>().FindAsync(filter);
            ProjectionResult = await result.FirstOrDefaultAsync();
        }
    }

    [Fact]
    void should_have_stored_generation_1_content() => Context.StoredEvent["content"].AsBsonDocument.Contains("1").ShouldBeTrue();

    [Fact]
    void should_have_stored_generation_2_content_from_upcast() => Context.StoredEvent["content"].AsBsonDocument.Contains("2").ShouldBeTrue();

    [Fact]
    void should_have_stored_generation_3_content_from_upcast() => Context.StoredEvent["content"].AsBsonDocument.Contains("3").ShouldBeTrue();

    [Fact]
    void should_have_split_first_name_in_generation_2() => Context.StoredEvent["content"].AsBsonDocument["2"].ToJson().ShouldContain("Jane");

    [Fact]
    void should_have_split_last_name_in_generation_2() => Context.StoredEvent["content"].AsBsonDocument["2"].ToJson().ShouldContain("Doe");

    [Fact]
    void should_have_default_email_in_generation_3() => Context.StoredEvent["content"].AsBsonDocument["3"].ToJson().ShouldContain("unknown@example.com");

    [Fact]
    void should_have_reactor_receive_first_name() => Context.Reactor.ReceivedFirstName.ShouldEqual("Jane");

    [Fact]
    void should_have_reactor_receive_last_name() => Context.Reactor.ReceivedLastName.ShouldEqual("Doe");

    [Fact]
    void should_have_reactor_receive_generation_2() => Context.Reactor.ReceivedGeneration.ShouldEqual(2u);

    [Fact]
    void should_have_reducer_receive_first_name() => Context.Reducer.ReceivedFirstName.ShouldEqual("Jane");

    [Fact]
    void should_have_reducer_receive_last_name() => Context.Reducer.ReceivedLastName.ShouldEqual("Doe");

    [Fact]
    void should_have_reducer_receive_generation_2() => Context.Reducer.ReceivedGeneration.ShouldEqual(2u);

    [Fact]
    void should_have_projection_set_first_name() => Context.ProjectionResult.FirstName.ShouldEqual("Jane");

    [Fact]
    void should_have_projection_set_last_name() => Context.ProjectionResult.LastName.ShouldEqual("Doe");
}
