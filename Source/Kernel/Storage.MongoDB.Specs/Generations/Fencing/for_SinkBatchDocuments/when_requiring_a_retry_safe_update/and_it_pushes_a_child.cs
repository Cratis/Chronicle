// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_SinkBatchDocuments.when_requiring_a_retry_safe_update;

public class and_it_pushes_a_child : Specification
{
    Exception? _error;

    void Because()
    {
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.DocumentSerializer.Returns(BsonDocumentSerializer.Instance);
        collection.Settings.Returns(new MongoCollectionSettings());
        collection.CollectionNamespace.Returns(new CollectionNamespace("specs", "orders"));
        _error = Catch.Exception(() => SinkBatchDocuments.RequireRetrySafe(collection, Builders<BsonDocument>.Update.Set("name", "x").Push("children", "c"), 2));
    }

    [Fact] void should_name_the_refusal() => _error.ShouldBeOfExactType<NonIdempotentWriteCannotBeSplit>();
    [Fact] void should_still_be_an_invalid_operation() => (_error is InvalidOperationException).ShouldBeTrue();
    [Fact] void should_name_only_the_blocking_operator() => ((NonIdempotentWriteCannotBeSplit)_error!).Operations.ShouldContainOnly("$push");
    [Fact] void should_carry_the_cap() => ((NonIdempotentWriteCannotBeSplit)_error!).MaxBatchSize.ShouldEqual(2);
    [Fact] void should_name_the_collection() => ((NonIdempotentWriteCannotBeSplit)_error!).Collection.ShouldEqual("orders");
    [Fact] void should_mention_the_setting() => _error!.Message.ShouldContain("ReadModels.MaxBatchSize");
    [Fact] void should_mention_the_operator() => _error!.Message.ShouldContain("$push");
}
