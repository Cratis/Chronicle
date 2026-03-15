// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_registered_migration.and_event_has_default_value.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_registered_migration;

[Collection(ChronicleCollection.Name)]
public class and_event_has_default_value(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(ContractSigned)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(ContractSignedMigrator)];

        public EventSourceId EventSourceId { get; } = "some-contract";
        public ContractSigned Event { get; private set; }
        public IAppendResult AppendResult { get; private set; }
        public BsonDocument StoredEvent { get; private set; }

        void Establish()
        {
            Event = new ContractSigned("c-001");
        }

        async Task Because()
        {
            AppendResult = await EventStore.EventLog.Append(EventSourceId, Event);
            var collection = EventStoreForNamespaceDatabase.Database.GetCollection<BsonDocument>("event-log");
            StoredEvent = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
        }
    }

    [Fact] void should_succeed() => Context.AppendResult.IsSuccess.ShouldBeTrue();
    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber(1);
    [Fact] void should_have_stored_generation_1_content() => Context.StoredEvent["content"].AsBsonDocument.Contains("1").ShouldBeTrue();
    [Fact] void should_have_stored_generation_2_content_via_upcast() => Context.StoredEvent["content"].AsBsonDocument.Contains("2").ShouldBeTrue();
    [Fact] void should_have_applied_default_status_in_gen2_content() =>
        Context.StoredEvent["content"].AsBsonDocument["2"].ToJson().ShouldContain("pending");
}
