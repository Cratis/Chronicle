// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.OrleansInProcess.for_EventSequence.when_appending;

[Collection(GlobalCollection.Name)]
public class existing_sequence_number(existing_sequence_number.context fixture) : OrleansTest<existing_sequence_number.context>(fixture)
{
    public class context(GlobalFixture globalFixture) : IntegrationTestSetup(globalFixture)
    {
        public Events.EventSourceId EventSourceId { get; } = "source";
        public SomeEvent FirstEvent { get; private set; }
        public SomeEvent SecondEvent { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        public override async Task Establish()
        {
            FirstEvent = new SomeEvent("some value");
            SecondEvent = new SomeEvent("some other value");
            await EventStore.EventLog.Append(EventSourceId, FirstEvent);
        }
        public override async Task Because()
        {
            var database = EventStoreForNamespaceDatabase.Database;
            var updateDef = Builders<Event>.Update.Set(_ => _.SequenceNumber, new EventSequenceNumber(1));
            database.GetCollection<Event>(WellKnownCollectionNames.EventLog).UpdateOne(_ => _.SequenceNumber == 0, updateDef);
            await EventStore.EventLog.Append(EventSourceId, SecondEvent);
        }
    }

    [Fact]
    Task should_have_correct_next_sequence_number() => Fixture.ShouldHaveCorrectNextSequenceNumber(2);

    [Fact]
    Task should_have_correct_tail_sequence_number() => Fixture.ShouldHaveCorrectTailSequenceNumber(1);

    [Fact]
    Task should_have_the_first_event_stored() => Fixture.ShouldHaveStoredCorrectEvent<SomeEvent>(0, Fixture.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Fixture.FirstEvent.Content));

    [Fact]
    Task should_have_the_second_event_stored() => Fixture.ShouldHaveStoredCorrectEvent<SomeEvent>(1, Fixture.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Fixture.SecondEvent.Content));
}
