// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_pii.an_event.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_pii;

[Collection(ChronicleCollection.Name)]
public class an_event(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "some-person";
        public SomeEventWithPII Event { get; private set; }
        public BsonDocument StoredEvent { get; private set; }
        public BsonDocument StoredEncryptionKey { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEventWithPII)];

        void Establish()
        {
            Event = new SomeEventWithPII("John Doe", "123-45-6789");
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);

            var eventCollection = EventStoreForNamespaceDatabase.Database.GetCollection<BsonDocument>("event-log");
            StoredEvent = await eventCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();

            var keyCollection = EventStoreForNamespaceDatabase.Database.GetCollection<BsonDocument>("encryption-keys");
            StoredEncryptionKey = await keyCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
        }
    }

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact]
    Task should_return_decrypted_content_when_reading_event() =>
        Context.ShouldHaveAppendedEvent<SomeEventWithPII>(
            EventSequenceNumber.First.Value,
            Context.EventSourceId.Value,
            readEvent =>
            {
                readEvent.Name.ShouldEqual(Context.Event.Name);
                readEvent.SocialSecurityNumber.ShouldEqual(Context.Event.SocialSecurityNumber);
            });

    [Fact]
    void should_have_stored_non_pii_content_in_clear_text() =>
        Context.StoredEvent["content"].AsBsonDocument["1"].ToJson().ShouldContain("John Doe");

    [Fact]
    void should_not_have_stored_pii_content_in_clear_text() =>
        Context.StoredEvent["content"].AsBsonDocument["1"].ToJson().Contains("123-45-6789").ShouldBeFalse();

    [Fact]
    void should_have_created_an_encryption_key_for_the_event_source() =>
        Context.StoredEncryptionKey["_id"].AsString.ShouldEqual(Context.EventSourceId.Value);
}
