// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_SinkBatchDocuments.when_requiring_a_retry_safe_update;

public class and_it_is_an_unmarked_pipeline : Specification
{
    Exception? _error;

    void Because()
    {
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.DocumentSerializer.Returns(BsonDocumentSerializer.Instance);
        collection.Settings.Returns(new MongoCollectionSettings());
        collection.CollectionNamespace.Returns(new CollectionNamespace("specs", "orders"));
        _error = Catch.Exception(() => SinkBatchDocuments.RequireRetrySafe(collection, new PipelineUpdateDefinition<BsonDocument>(new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>([new BsonDocument("$set", new BsonDocument("name", "x"))])), 2));
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<NonIdempotentWriteCannotBeSplit>();
}
