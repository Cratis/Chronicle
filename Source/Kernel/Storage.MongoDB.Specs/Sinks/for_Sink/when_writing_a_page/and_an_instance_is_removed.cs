// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_writing_a_page;

/// <summary>
/// A reducer that returns no state for a partition removes its read model, in the same write as the rest of the page.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> supplying the database.</param>
[Collection(MongoDBCollection.Name)]
public class and_an_instance_is_removed(MongoDBFixture fixture) : given.a_page_sink(fixture)
{
    FailedPartition[] _failures;
    BsonDocument? _removed;
    BsonDocument? _other;

    async Task Establish() => await _collection.InsertOneAsync(new BsonDocument { { "_id", "first" }, { "count", 1 } });

    async Task Because()
    {
        _failures = [.. await Pages.ApplyPage([Removal("first", 2), Write("second", 2)])];
        _removed = await Document("first");
        _other = await Document("second");
    }

    [Fact] void should_report_no_failures() => _failures.ShouldBeEmpty();
    [Fact] void should_delete_the_instance() => _removed.ShouldBeNull();
    [Fact] void should_write_the_rest_of_the_page() => _other!["count"].AsInt32.ShouldEqual(2);
    [Fact] void should_write_the_page_in_one_bulk_write() => _bulkWrites.ShouldEqual(1);
}
