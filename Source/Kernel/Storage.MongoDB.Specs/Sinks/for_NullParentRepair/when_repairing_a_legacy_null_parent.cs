// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_NullParentRepair;

public class when_repairing_a_legacy_null_parent : Specification
{
    IMongoCollection<BsonDocument> _collection;
    List<FilterDefinition<BsonDocument>> _filters;
    List<UpdateDefinition<BsonDocument>> _updates;
    int _retries;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _filters = [];
        _updates = [];
        _collection.UpdateOneAsync(
            Arg.Do<FilterDefinition<BsonDocument>>(_filters.Add),
            Arg.Do<UpdateDefinition<BsonDocument>>(_updates.Add),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<UpdateResult>(null!));
    }

    async Task Because() => await NullParentRepair.RepairAndRetry(
        _collection,
        BsonString.Create("probe-1"),
        Builders<BsonDocument>.Update.Set("outer.info.name", "ciphertext"),
        () =>
        {
            _retries++;
            return Task.CompletedTask;
        });

    [Fact] void should_unset_each_ancestor_once() => _updates.Count.ShouldEqual(2);
    [Fact] void should_unset_the_outer_ancestor_first() => Render(_updates[0])["$unset"].AsBsonDocument.Contains("outer").ShouldBeTrue();
    [Fact] void should_unset_the_inner_ancestor() => Render(_updates[1])["$unset"].AsBsonDocument.Contains("outer.info").ShouldBeTrue();
    [Fact] void should_filter_every_repair_to_the_document_and_null_parent() => _filters.TrueForAll(filter => Render(filter).ToJson().Contains("probe-1") && Render(filter).ToJson().Contains("$exists") && Render(filter).ToJson().Contains("null")).ShouldBeTrue();
    [Fact] void should_retry_the_original_write_once() => _retries.ShouldEqual(1);

    static BsonDocument Render(UpdateDefinition<BsonDocument> update) => update.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
    static BsonDocument Render(FilterDefinition<BsonDocument> filter) => filter.Render(new RenderArgs<BsonDocument>(BsonSerializer.LookupSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;
}
