// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.for_ContextStorageIsolation.when_a_previous_host_writes_late.and_a_new_context_has_projected_events.context;

namespace Cratis.Chronicle.Integration.for_ContextStorageIsolation.when_a_previous_host_writes_late;

[Collection(ChronicleCollection.Name)]
public class and_a_new_context_has_projected_events(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : projected_context(fixture)
    {
#pragma warning disable CA2213 // The second context is disposed in Destroy.
        projected_context? _current;
#pragma warning restore CA2213

        public ContextCount Result;
        public long DocumentCount;
        public long LateWriteCount;
        public string[] DatabaseNames = [];
        public string CurrentPrefix = string.Empty;
        public string PreviousPrefix = string.Empty;

        async Task Because()
        {
            if (!IsIsolatedMongoDB)
            {
                return;
            }

            PreviousPrefix = DatabaseNamePrefix;
            var previousDatabaseName = ReadModelsDatabase.Database.DatabaseNamespace.DatabaseName;
            var collectionName = Services.GetRequiredService<INamingPolicy>().GetReadModelName(typeof(ContextCount));

            // Finish context A before beginning context B. The late write below is deliberate and deterministic,
            // not an attempt to reproduce the shutdown race on the clock.
            await DisposeAsync();
            _current = new(ChronicleFixture);
            await _current.InitializeAsync();
            CurrentPrefix = _current.DatabaseNamePrefix;

            using var client = new MongoClient($"mongodb://{MongoDBContainer.Hostname}:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true");
            var previousCollection = client.GetDatabase(previousDatabaseName).GetCollection<BsonDocument>(collectionName);
            await previousCollection.ReplaceOneAsync(
                new BsonDocument("_id", EventSource.Value),
                new BsonDocument { { "_id", EventSource.Value }, { "count", 999 } },
                new ReplaceOptions { IsUpsert = true });

            LateWriteCount = await previousCollection.CountDocumentsAsync(new BsonDocument("count", 999));
            Result = await _current.EventStore.ReadModels.GetInstanceById<ContextCount>(EventSource.Value);
            DocumentCount = await _current.ReadModelsDatabase.Database.GetCollection<BsonDocument>(collectionName).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            DatabaseNames =
            [
                _current.EventStoreDatabase.Database.DatabaseNamespace.DatabaseName,
                _current.EventStoreForNamespaceDatabase.Database.DatabaseNamespace.DatabaseName,
                _current.ReadModelsDatabase.Database.DatabaseNamespace.DatabaseName
            ];
        }

        async Task Destroy()
        {
            if (_current is not null)
            {
                await _current.DisposeAsync();
            }
        }
    }

    public class projected_context(ChronicleFixture fixture) : Specification(fixture)
    {
        public EventSourceId EventSource { get; } = "same-source";
        public bool IsIsolatedMongoDB => ChronicleFixture.UsesPerContextMongoDBHost;
        public override IEnumerable<Type> EventTypes => [typeof(ContextEventRecorded)];
        public override IEnumerable<Type> Projections => [typeof(ContextCountProjection)];

        async Task Establish()
        {
            // These storage-specific assertions are inert in the out-of-process and non-MongoDB lanes.
            if (!IsIsolatedMongoDB)
            {
                return;
            }

            var handler = EventStore.Projections.GetHandlerFor<ContextCountProjection>();
            await handler.WaitTillActive();
            await EventStore.EventLog.Append(EventSource, new ContextEventRecorded());
            var result = await EventStore.EventLog.Append(EventSource, new ContextEventRecorded());
            await handler.WaitTillReachesEventSequenceNumber(result.SequenceNumber);
        }
    }

    [Fact]
    void should_keep_the_current_projected_count()
    {
        if (Context.IsIsolatedMongoDB) Context.Result.Count.ShouldEqual(2);
    }

    [Fact]
    void should_keep_one_current_document()
    {
        if (Context.IsIsolatedMongoDB) Context.DocumentCount.ShouldEqual(1);
    }

    [Fact]
    void should_actually_write_the_conflicting_document_to_previous_storage()
    {
        if (Context.IsIsolatedMongoDB) Context.LateWriteCount.ShouldEqual(1);
    }

    [Fact]
    void should_use_a_different_prefix_for_the_current_context()
    {
        if (Context.IsIsolatedMongoDB) Context.CurrentPrefix.ShouldNotEqual(Context.PreviousPrefix);
    }

    [Fact]
    void should_prefix_every_current_reader_database()
    {
        if (Context.IsIsolatedMongoDB) Context.DatabaseNames.All(name => name.StartsWith(Context.CurrentPrefix, StringComparison.Ordinal)).ShouldBeTrue();
    }
}
