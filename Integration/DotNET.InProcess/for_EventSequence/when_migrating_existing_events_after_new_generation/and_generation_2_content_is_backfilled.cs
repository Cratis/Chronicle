// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Jobs;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_migrating_existing_events_after_new_generation.and_generation_2_content_is_backfilled.context;
using KernelConcepts = Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_migrating_existing_events_after_new_generation;

[Collection(ChronicleCollection.Name)]
public class and_generation_2_content_is_backfilled(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(OrderPlacedV1), typeof(OrderPlaced)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(OrderPlacedMigrator)];

        public EventSourceId EventSourceId { get; } = "some-order";
        public OrderPlacedV1 Event { get; private set; }
        public BsonDocument StoredEventBeforeMigration { get; private set; }
        public BsonDocument StoredEventAfterMigration { get; private set; }

        void Establish()
        {
            Event = new OrderPlacedV1("Widget order");
        }

        async Task Because()
        {
            // 1. Append gen 1 event — will automatically have gen 1 + gen 2 content.
            await EventStore.EventLog.Append(EventSourceId, Event);

            // 2. Remove gen 2 content from the stored event to simulate a legacy event.
            var collection = EventStoreForNamespaceDatabase.Database.GetCollection<BsonDocument>("event-log");
            var update = Builders<BsonDocument>.Update.Unset("content.2");
            await collection.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty, update);

            StoredEventBeforeMigration = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();

            // 3. Start the migration job.
            var grainFactory = Services.GetRequiredService<IGrainFactory>();
            var jobsManager = grainFactory.GetJobsManager(
                (KernelConcepts.EventStoreName)"testing",
                KernelConcepts.EventStoreNamespaceName.Default);

            var result = await jobsManager.Start<IMigrateExistingEventsForType, MigrateExistingEventsForTypeRequest>(
                new MigrateExistingEventsForTypeRequest("OrderPlaced"));

            // 4. Wait for the migration job to complete.
            if (result.IsSuccess)
            {
                var timeout = DateTime.UtcNow.AddSeconds(30);
                while (DateTime.UtcNow < timeout)
                {
                    var jobs = await jobsManager.GetAllJobs();
                    var migrationJob = jobs.FirstOrDefault(j => j.Id == result.AsT0);
                    if (migrationJob?.Status is KernelConcepts.Jobs.JobStatus.CompletedSuccessfully or KernelConcepts.Jobs.JobStatus.CompletedWithFailures)
                    {
                        break;
                    }

                    await Task.Delay(100);
                }
            }

            // 5. Read the stored event after migration.
            StoredEventAfterMigration = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
        }
    }

    [Fact]
    void should_not_have_generation_2_content_before_migration() =>
        Context.StoredEventBeforeMigration["content"].AsBsonDocument.Contains("2").ShouldBeFalse();

    [Fact]
    void should_have_generation_1_content_after_migration() =>
        Context.StoredEventAfterMigration["content"].AsBsonDocument.Contains("1").ShouldBeTrue();

    [Fact]
    void should_have_generation_2_content_after_migration() =>
        Context.StoredEventAfterMigration["content"].AsBsonDocument.Contains("2").ShouldBeTrue();

    [Fact]
    void should_have_original_description_in_generation_1() =>
        Context.StoredEventAfterMigration["content"].AsBsonDocument["1"].ToJson().ShouldContain("Widget order");

    [Fact]
    void should_have_description_in_generation_2() =>
        Context.StoredEventAfterMigration["content"].AsBsonDocument["2"].ToJson().ShouldContain("Widget order");

    [Fact]
    void should_have_default_status_in_generation_2() =>
        Context.StoredEventAfterMigration["content"].AsBsonDocument["2"].ToJson().ShouldContain("pending");
}
