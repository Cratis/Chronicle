// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_SinkBatchDocuments.when_requiring_a_retry_safe_update;

public class and_a_marked_pipeline_has_another_stage : Specification
{
    Exception? _error;

    void Because()
    {
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.DocumentSerializer.Returns(BsonDocumentSerializer.Instance);
        collection.Settings.Returns(new MongoCollectionSettings());
        collection.CollectionNamespace.Returns(new CollectionNamespace("specs", "orders"));
        _error = Catch.Exception(() => SinkBatchDocuments.RequireRetrySafe(collection, MarkedWithExtraStage(), 2));
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<NonIdempotentWriteCannotBeSplit>();

    static UpdateDefinition<BsonDocument> MarkedWithExtraStage()
    {
        var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>([new BsonDocument("$replaceWith", new BsonDocument("name", "x"))]);
        return AppliedEventMarkerUpdates.Mark(new PipelineUpdateDefinition<BsonDocument>(pipeline));
    }
}
