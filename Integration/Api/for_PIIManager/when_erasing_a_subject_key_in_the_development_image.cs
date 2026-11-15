// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.Api.for_PIIManager.when_erasing_a_subject_key_in_the_development_image.context;

namespace Cratis.Chronicle.Integration.Api.for_PIIManager;

[Collection(ChronicleCollection.Name)]
public class when_erasing_a_subject_key_in_the_development_image(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixtureWithLocalImage fixture) : given.Specification(fixture)
    {
        readonly Subject _subject = $"subject-{Guid.NewGuid():N}";

        public BsonDocument Erasure;
        public string KernelLogs;

        public override IEnumerable<Type> EventTypes => [typeof(SubjectRegistered)];

        async Task Establish() =>
            await EventStore.EventLog.Append(Guid.NewGuid().ToString(), new SubjectRegistered(_subject, "protected-value"));

        async Task Because()
        {
            await EventStore.PII.DeleteEncryptionKeyFor(_subject).WaitAsync(TimeSpan.FromSeconds(30));
            Erasure = await WaitForConfirmedErasure();
            var (stdout, stderr) = await ChronicleFixture.MongoDBContainer.GetLogsAsync().WaitAsync(TimeSpan.FromSeconds(10));
            KernelLogs = stdout + stderr;
        }

        async Task<BsonDocument> WaitForConfirmedErasure()
        {
            // Reuse the fixture's MongoDB client, but address the SDK's scenario-specific namespace.
            var databaseName = DatabaseNames.ForEventStoreNamespace(EventStore.Name.Value, EventStore.Namespace.Value);
            var database = EventStoreForNamespaceDatabase.Database.Client.GetDatabase(databaseName);
            var erasures = database.GetCollection<BsonDocument>(WellKnownCollectionNames.EncryptionKeyErasures);
            var filter = new BsonDocument
            {
                { "_id", _subject.Value },
                { "requests", new BsonDocument("$elemMatch", new BsonDocument("confirmed", true)) }
            };
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (true)
            {
                var erasure = await erasures.Find(filter).SingleOrDefaultAsync(timeout.Token);
                if (erasure is not null) return erasure;
                await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
            }
        }
    }

    [Fact]
    void should_have_a_confirmed_erasure_request() =>
        Context.Erasure["requests"].AsBsonArray.Single()["confirmed"].AsBoolean.ShouldBeTrue();

    [Fact]
    void should_not_warn_about_standalone_mongodb() =>
        Context.KernelLogs.ShouldNotContain("MongoDB is a standalone server without replication");
}
