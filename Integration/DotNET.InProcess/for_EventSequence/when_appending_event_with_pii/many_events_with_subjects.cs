// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_pii.many_events_with_subjects.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_pii;

[Collection(ChronicleCollection.Name)]
public class many_events_with_subjects(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "request-1";
        public EventWithSubjectAndPII FirstEvent { get; private set; }
        public EventWithSubjectAndPII SecondEvent { get; private set; }
        public List<BsonDocument> StoredEvents { get; private set; }
        public List<BsonDocument> StoredEncryptionKeys { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(EventWithSubjectAndPII)];

        void Establish()
        {
            FirstEvent = new EventWithSubjectAndPII("person-1", "Jane Doe", "111-22-3333");
            SecondEvent = new EventWithSubjectAndPII("person-2", "John Doe", "222-33-4444");
        }

        async Task Because()
        {
            await EventStore.EventLog.AppendMany(EventSourceId, [FirstEvent, SecondEvent]);

            var eventCollection = EventStoreForNamespaceDatabase.Database.GetCollection<BsonDocument>("event-log");
            StoredEvents = await eventCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

            var keyCollection = EventStoreForNamespaceDatabase.Database.GetCollection<BsonDocument>("encryption-keys");
            StoredEncryptionKeys = await keyCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        }
    }

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First + 1);

    [Fact]
    Task should_return_decrypted_first_content_when_reading_event() =>
        Context.ShouldHaveAppendedEvent<EventWithSubjectAndPII>(
            EventSequenceNumber.First,
            Context.EventSourceId,
            readEvent =>
            {
                readEvent.PersonId.ShouldEqual(Context.FirstEvent.PersonId);
                readEvent.Name.ShouldEqual(Context.FirstEvent.Name);
                readEvent.SocialSecurityNumber.ShouldEqual(Context.FirstEvent.SocialSecurityNumber);
            });

    [Fact]
    Task should_return_decrypted_second_content_when_reading_event() =>
        Context.ShouldHaveAppendedEvent<EventWithSubjectAndPII>(
            EventSequenceNumber.First + 1,
            Context.EventSourceId,
            readEvent =>
            {
                readEvent.PersonId.ShouldEqual(Context.SecondEvent.PersonId);
                readEvent.Name.ShouldEqual(Context.SecondEvent.Name);
                readEvent.SocialSecurityNumber.ShouldEqual(Context.SecondEvent.SocialSecurityNumber);
            });

    [Fact]
    void should_not_have_stored_first_pii_content_in_clear_text() =>
        Context.StoredEvents[0]["content"].AsBsonDocument["1"].ToJson().Contains(Context.FirstEvent.SocialSecurityNumber).ShouldBeFalse();

    [Fact]
    void should_not_have_stored_second_pii_content_in_clear_text() =>
        Context.StoredEvents[1]["content"].AsBsonDocument["1"].ToJson().Contains(Context.SecondEvent.SocialSecurityNumber).ShouldBeFalse();

    [Fact]
    void should_have_created_encryption_keys_for_the_subjects() =>
        Context.StoredEncryptionKeys
            .Select(key => key["_id"].AsBsonDocument["Identifier"].AsString)
            .Order()
            .ShouldEqual([Context.FirstEvent.PersonId, Context.SecondEvent.PersonId]);

    [Fact]
    void should_not_have_created_an_encryption_key_for_the_event_source() =>
        Context.StoredEncryptionKeys
            .Select(key => key["_id"].AsBsonDocument["Identifier"].AsString)
            .ShouldNotContain(Context.EventSourceId.Value);
}
