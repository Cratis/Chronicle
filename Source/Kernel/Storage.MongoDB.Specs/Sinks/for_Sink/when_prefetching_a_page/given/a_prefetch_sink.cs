// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page.given;

public abstract class a_prefetch_sink(MongoDBFixture fixture) : when_applying_changes_immediately.given.a_bulk_sink(fixture)
{
    protected readonly Key _key = new("k", ArrayIndexers.NoIndexers);
    protected int _reads;
    protected int _writes;
    protected ISinkPagePrefetch PrefetchSink => (ISinkPagePrefetch)_sink;

    protected override IMongoCollection<BsonDocument> InterceptCollection(IMongoCollection<BsonDocument> collection)
    {
        var intercepted = Substitute.For<IMongoCollection<BsonDocument>>();
        intercepted.CollectionNamespace.Returns(collection.CollectionNamespace);
        intercepted.FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                _reads++;
                return collection.FindAsync(info.ArgAt<FilterDefinition<BsonDocument>>(0), info.ArgAt<FindOptions<BsonDocument, BsonDocument>>(1));
            });
        intercepted.DeleteOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<CancellationToken>())
            .Returns(info => collection.DeleteOneAsync(info.ArgAt<FilterDefinition<BsonDocument>>(0), info.ArgAt<CancellationToken>(1)));
        intercepted.BulkWriteAsync(Arg.Any<IEnumerable<WriteModel<BsonDocument>>>(), Arg.Any<BulkWriteOptions>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                _writes++;
                return collection.BulkWriteAsync(info.ArgAt<IEnumerable<WriteModel<BsonDocument>>>(0), info.ArgAt<BulkWriteOptions>(1));
            });
        return intercepted;
    }
}
