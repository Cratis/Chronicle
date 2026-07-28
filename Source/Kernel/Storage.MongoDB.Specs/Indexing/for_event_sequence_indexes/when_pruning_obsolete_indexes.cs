// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.for_event_sequence_indexes;

[Collection(MongoDBCollection.Name)]
public class when_pruning_obsolete_indexes(MongoDBFixture fixture) : given.a_real_namespace_database(fixture)
{
    IReadOnlyList<string> _before;
    IReadOnlyList<string> _after;

    async Task Because()
    {
        var collectionName = _database.GetEventSequenceCollectionFor(EventSequenceId.Log).CollectionNamespace.CollectionName;
        var collection = _rawDatabase.GetCollection<Event>(collectionName);

        await collection.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Event>(
                Builders<Event>.IndexKeys.Text(_ => _.EventSourceId),
                new CreateIndexOptions { Name = "eventSourceId" }),
            new CreateIndexModel<Event>(
                Builders<Event>.IndexKeys.Wildcard(_ => _.ContentHashes),
                new CreateIndexOptions { Name = "contentHashes" }),
            new CreateIndexModel<Event>(
                Builders<Event>.IndexKeys.Ascending(_ => _.Type),
                new CreateIndexOptions { Name = "eventTypeId" }),
            new CreateIndexModel<Event>(
                Builders<Event>.IndexKeys.Ascending(_ => _.EventStreamType),
                new CreateIndexOptions { Name = "eventStreamType" })
        ]);

        _before = await IndexNamesFor(collectionName);
        await _database.EnsureIndexesForEventSequence(EventSequenceId.Log);
        _after = await IndexNamesFor(collectionName);
    }

    [Fact] void should_have_started_with_the_text_index() => _before.ShouldContain("eventSourceId");
    [Fact] void should_have_started_with_the_wildcard_index() => _before.ShouldContain("contentHashes");
    [Fact] void should_drop_the_text_index() => _after.ShouldNotContain("eventSourceId");
    [Fact] void should_drop_the_wildcard_index() => _after.ShouldNotContain("contentHashes");
    [Fact] void should_drop_the_single_field_event_type_index() => _after.ShouldNotContain("eventTypeId");
    [Fact] void should_drop_the_single_field_event_stream_type_index() => _after.ShouldNotContain("eventStreamType");
    [Fact] void should_create_the_per_type_tail_index() => _after.ShouldContain("type_sequenceNumber");
}
