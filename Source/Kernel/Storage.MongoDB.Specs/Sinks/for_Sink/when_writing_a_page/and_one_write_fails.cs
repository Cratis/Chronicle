// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_writing_a_page;

[Collection(MongoDBCollection.Name)]
public class and_one_write_fails(MongoDBFixture fixture) : given.a_page_sink(fixture)
{
    FailedPartition[] _failures;

    async Task Establish()
    {
        await _collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("count"), new CreateIndexOptions { Unique = true }));
        await _collection.InsertOneAsync(new BsonDocument { { "_id", "existing" }, { "count", 2 } });
    }

    async Task Because() => _failures = [.. await Pages.ApplyPage([Write("first", 1), Write("second", 2), Write("third", 3)])];

    [Fact] void should_report_only_the_failed_partition() => _failures.Select(_ => _.EventSourceId.Value).ShouldContainOnly("second");
    [Fact] void should_know_the_failed_write_was_not_applied() => _failures.Single().OutcomeUnknown.ShouldBeFalse();
    [Fact] async Task should_write_the_partition_before_it() => (await Document("first"))!["count"].AsInt32.ShouldEqual(1);
    [Fact] async Task should_write_the_partition_after_it() => (await Document("third"))!["count"].AsInt32.ShouldEqual(3);
    [Fact] async Task should_not_write_the_failed_partition() => (await Document("second")).ShouldBeNull();
}
