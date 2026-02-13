// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending.existing_sequence_number.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class existing_sequence_number(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEvent FirstEvent;
        public SomeEvent SecondEvent;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Establish()
        {
            FirstEvent = new SomeEvent("some value");
            SecondEvent = new SomeEvent("some other value");
            var database = EventStoreForNamespaceDatabase.Database;
            var @event = new Event(
                0,
                CorrelationId.New(),
                [],
                [],
                typeof(SomeEvent).GetEventType().Id.Value,
                DateTimeOffset.UtcNow,
                Concepts.Events.EventSourceType.Default,
                EventSourceId.Value,
                Concepts.Events.EventStreamType.All,
                Concepts.Events.EventStreamId.Default,
                [],
                new Dictionary<string, BsonDocument>() { { "1", FirstEvent.ToBsonDocument() } },
                new Dictionary<string, string>() { { "1", EventHash.NotSet } },
                []);

            await database.GetCollection<Event>(WellKnownCollectionNames.EventLog).InsertOneAsync(@event);
        }

        async Task Because() => await EventStore.EventLog.Append(EventSourceId, SecondEvent);
    }

    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber(2);
    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(1);
    [Fact] Task should_have_the_first_event_stored() => Context.ShouldHaveAppendedEvent<SomeEvent>(0, Context.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Context.FirstEvent.Content));
    [Fact] Task should_have_the_second_event_stored() => Context.ShouldHaveAppendedEvent<SomeEvent>(1, Context.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Context.SecondEvent.Content));
}
