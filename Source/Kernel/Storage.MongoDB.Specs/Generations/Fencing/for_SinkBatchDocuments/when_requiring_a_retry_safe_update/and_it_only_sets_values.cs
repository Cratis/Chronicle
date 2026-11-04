// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_SinkBatchDocuments.when_requiring_a_retry_safe_update;

public class and_it_only_sets_values : Specification
{
    IMongoCollection<BsonDocument> _collection = null!;
    Exception? _error;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<BsonDocument>>();
        _collection.DocumentSerializer.Returns(BsonDocumentSerializer.Instance);
        _collection.Settings.Returns(new MongoCollectionSettings());
        _collection.CollectionNamespace.Returns(new CollectionNamespace("specs", "orders"));
        _collection.ClearReceivedCalls();
    }

    void Because() => _error = Catch.Exception(() => SinkBatchDocuments.RequireRetrySafe(_collection, Builders<BsonDocument>.Update.Set("name", "x").Unset("old"), 2));

    [Fact] void should_not_refuse() => _error.ShouldBeNull();
    [Fact] void should_not_look_up_the_collection_name() => _ = _collection.DidNotReceive().CollectionNamespace;
}
